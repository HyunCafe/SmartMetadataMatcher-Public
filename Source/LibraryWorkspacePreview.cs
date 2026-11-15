using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    // Design C workspace: read-only scans and explicitly approved single/confirmed-batch imports.
    // Native IGDB scanning may persist lookup-cache records only; marking or
    // scanning never approves an import. All writes use the shared journaled writer.
    // A saved candidate is never marked ready for an automatic metadata import here.
    internal sealed class LibraryWorkspacePreview
    {
        private readonly IPlayniteAPI api;
        private readonly string dataPath;
        private readonly WorkspaceReturnState returnState;
        private Window window;
        private DataGrid table;
        private ComboBox scope;
        private TextBox search;
        private TextBlock totals, notice;
        private Border noticeFrame;
        private TextBlock metricSuggestions, metricChoices, metricPending, metricUpdated, metricOther, metricAll;
        private readonly List<Button> navigation = new List<Button>();
        private TextBlock detailTitle, detailPlatform, detailStatus, detailMatch, detailMissing, detailHelp;
        private TextBlock detailVerification, detailEvidence;
        private Button refreshFocusedButton, refreshMarkedButton;
        private Button identitySearchButton, identityConfirmButton, identityForgetButton;
        private TextBlock detailFieldSummary, selectionSummary, listSummary, artworkNote;
        private TextBlock liveFieldStatus;
        private Button checkLiveFieldsButton, editDraftButton, reviewFocusedButton, ignoreRestoreButton;
        private TextBlock draftStatus;
        // Session-only proposal state; never import consent or a persisted decision.
        private readonly Dictionary<Guid, WorkspaceDraftState> draftStates =
            new Dictionary<Guid, WorkspaceDraftState>();
        private WorkspaceLiveComparisonResult liveFieldResult;
        private CancellationTokenSource liveFieldCancellation;
        private bool checkingLiveFields;
        private bool checkingBatchFields;
        private bool nativeApplying;
        private CancellationTokenSource nativeImportCancellation;
        private CancellationTokenSource batchFieldCancellation;
        private TextBlock markTraySummary;
        private DataGrid fieldComparison;
        private Button previousGameButton, nextGameButton, reviewMarksButton;
        private Expander artworkExpander;
        // One existing filter control handles platforms and optional Playnite
        // source facets; no additional toolbar row or trusted identity input.
        private const string SourceFacetPrefix = "Source: ";
        private ComboBox platformFilter, missingFilter;
        private Button toolbarResetButton, searchClearButton;
        private TextBlock searchPlaceholder;
        private const double ToolbarHeight = 36;
        private const double ToolbarGap = 8;
        private const double ToolbarGroupGap = 16;
        private Button markHighlightedButton, unmarkHighlightedButton, clearMarkedButton, bulkActionsButton;
        private bool onlyIgnoredView;
        private Image headlineCover, existingCover, existingIcon, existingBackground, suggestedCover, suggestedScreenshot;
        private TextBlock headlineArtFallback;
        private Button suggestedArtButton;
        // Strong eligibility is ephemeral and MUST originate in a fresh resolver run,
        // never a saved score/status, copied IGDB ID, or persisted decision.
        private sealed class FreshStrictEvidence
        {
            internal CatalogInput Snapshot;
            internal long IgdbId;
            internal string CatalogTitle, CacheTimestamp;
            internal bool Matches(CatalogInput current, string id, string title, string timestamp)
            {
                long value;
                return Snapshot != null && current != null &&
                    Snapshot.Id == current.Id && Snapshot.Id != Guid.Empty &&
                    String.Equals(Snapshot.Name,current.Name,StringComparison.Ordinal) &&
                    String.Equals(Snapshot.Platform,current.Platform,StringComparison.Ordinal) &&
                    Snapshot.MissingMask == current.MissingMask &&
                    Snapshot.MissingCount == current.MissingCount &&
                    Snapshot.LocalReleaseYear == current.LocalReleaseYear &&
                    Int64.TryParse(id,out value) && value == IgdbId &&
                    String.Equals(CatalogTitle,title,StringComparison.Ordinal) &&
                    String.Equals(CacheTimestamp,timestamp,StringComparison.Ordinal);
            }
        }
        private readonly Dictionary<Guid,FreshStrictEvidence> freshStrictEvidence =
            new Dictionary<Guid,FreshStrictEvidence>();
        private bool FreshStrictReady(LibraryWorkspaceItem row, CatalogInput input)
        {
            if (row == null || input == null || !row.FromSaved) return false;
            FreshStrictEvidence proof;
            return freshStrictEvidence.TryGetValue(row.GameId,out proof) && proof != null &&
                proof.Matches(input,row.CachedCatalogId,row.CachedCatalogName,row.CachedScannedUtc);
        }
        // UI enablement must tolerate a game disappearing or changing while
        // the independent read-only field-check worker is finishing.
        private bool NativeBatchReady(LibraryWorkspaceItem row)
        {
            if (row == null) return false;
            Game game;
            if (!knownGames.TryGetValue(row.GameId,out game) || game==null) return false;
            try { return row.IdentityConfirmed || FreshStrictReady(row,ScanInput(game)); }
            catch (Exception) { return false; }
        }
        // In-memory selection only. Handing off IDs never authorizes a write.
        private readonly HashSet<Guid> markedIds = new HashSet<Guid>();
        private readonly Dictionary<Guid, Game> knownGames = new Dictionary<Guid, Game>();
        private readonly List<LibraryWorkspaceItem> items = new List<LibraryWorkspaceItem>();
        private const string PreviewLabel = "Library Workspace (development preview)";
        private Button scanPendingButton, scanAllPendingButton, cancelScanButton, settingsButton;
        private TextBlock scanStatus;
        private CancellationTokenSource scanCancellation;
        private bool scanning;

        internal LibraryWorkspacePreview(IPlayniteAPI api, string dataPath)
            : this(api, dataPath, null) { }

        // An in-memory return point, not persisted work or approval evidence.
        internal LibraryWorkspacePreview(IPlayniteAPI api, string dataPath,
            WorkspaceReturnState restore)
        {
            if (api == null) throw new ArgumentNullException("api");
            if (String.IsNullOrWhiteSpace(dataPath)) throw new ArgumentException("Extension data path is required");
            this.api = api;
            this.dataPath = dataPath;
            returnState = restore;
        }

        private static Brush Color(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
        // Retain the classic Matcher's deep-blue and cyan identity, with more
        // restrained layout and clearer status colors. Color is never the only
        // indication of what a game needs: every state also has a text label.
        private static readonly Brush Background = Color(10, 18, 37);
        private static readonly Brush Panel = Color(15, 27, 51);
        private static readonly Brush PanelRaised = Color(20, 37, 65);
        private static readonly Brush Accent = Color(50, 193, 240);
        private static readonly Brush Text = Color(227, 237, 248);
        private static readonly Brush Muted = Color(170, 186, 207);
        private static readonly Brush Stroke = Color(55, 103, 145);
        private static readonly Brush SoftStroke = Color(38, 65, 96);
        private static readonly Brush Good = Color(102, 225, 148);
        private static readonly Brush Warning = Color(231, 182, 105);
        private static readonly Brush Bad = Color(244, 154, 140);
        private static readonly Brush Selection = Color(23, 69, 98);
        private static readonly Brush AlternateRow = Color(16, 31, 54);

        private static TextBlock Label(string text, double size, Brush color)
        {
            return new TextBlock {Text = text, FontFamily = new FontFamily("Segoe UI"),
                FontSize = size, Foreground = color, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center};
        }
        private static TextBlock Symbol(string glyph, Brush color, double size = 16)
        {
            // Segoe MDL2 Assets ships with supported Windows desktop versions.
            // Icons supplement, rather than replace, readable text labels.
            return new TextBlock {Text = glyph, FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = size, Foreground = color, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)};
        }
        private static StackPanel IconLabel(string glyph, string title, Brush color, double size = 13)
        {
            var pair = new StackPanel {Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center};
            pair.Children.Add(Symbol(glyph, color));
            var caption = Label(title, size, color);
            caption.FontWeight = FontWeights.SemiBold;
            pair.Children.Add(caption);
            return pair;
        }
        // Review plans and exact-field comparisons are documents, not row actions.
        // Keep text tooltips and scrollbars usable without selectable-looking rows.
        // The main library grid and IGDB identity chooser remain fully interactive.
        private static void ConfigureReadOnlyReviewGrid(DataGrid grid)
        {
            grid.IsTabStop = false;
            grid.Focusable = false;
            grid.IsSynchronizedWithCurrentItem = false;
            grid.SelectionMode = DataGridSelectionMode.Single;
            grid.SelectionUnit = DataGridSelectionUnit.FullRow;
            grid.RowBackground = Panel;
            grid.AlternatingRowBackground = Panel;
            grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
            grid.HorizontalGridLinesBrush = SoftStroke;

            MouseButtonEventHandler preventRowSelection = (sender, args) => {
                var hit = args.OriginalSource as DependencyObject;
                while (hit != null && hit != grid && !(hit is DataGridRow))
                    hit = VisualTreeHelper.GetParent(hit);
                if (hit is DataGridRow) args.Handled = true;
            };
            grid.PreviewMouseLeftButtonDown += preventRowSelection;
            grid.PreviewMouseRightButtonDown += preventRowSelection;

            var cells = new Style(typeof(DataGridCell));
            cells.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cells.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            cells.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            cells.Setters.Add(new Setter(UIElement.FocusableProperty, false));
            var hoveredCell = new Trigger {Property = UIElement.IsMouseOverProperty, Value = true};
            hoveredCell.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cells.Triggers.Add(hoveredCell);
            var selectedCell = new Trigger {Property = DataGridCell.IsSelectedProperty, Value = true};
            selectedCell.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            selectedCell.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            cells.Triggers.Add(selectedCell);
            grid.CellStyle = cells;

            var rows = new Style(typeof(DataGridRow));
            rows.Setters.Add(new Setter(Control.BackgroundProperty, Panel));
            rows.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            rows.Setters.Add(new Setter(UIElement.FocusableProperty, false));
            var hoveredRow = new Trigger {Property = UIElement.IsMouseOverProperty, Value = true};
            hoveredRow.Setters.Add(new Setter(Control.BackgroundProperty, Panel));
            rows.Triggers.Add(hoveredRow);
            var selectedRow = new Trigger {Property = DataGridRow.IsSelectedProperty, Value = true};
            selectedRow.Setters.Add(new Setter(Control.BackgroundProperty, Panel));
            rows.Triggers.Add(selectedRow);
            grid.RowStyle = rows;
        }

        private static Button ActionButton(string caption, string glyph, Action action, bool primary = false)
        {
            var color = primary ? Background : Text;
            var button = new Button {Content = IconLabel(glyph, caption, color),
                Padding = new Thickness(12, 0, 12, 0), MinHeight = ToolbarHeight,
                Height = ToolbarHeight, Margin = new Thickness(0, 0, ToolbarGap, 0), FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = primary ? Accent : PanelRaised, Foreground = color,
                BorderBrush = primary ? Accent : Stroke, BorderThickness = new Thickness(1)};
            button.Click += (sender, args) => action();
            return button;
        }
        private static Button CompactToolbarButton(string caption, string glyph, Action action,
            double width, string tooltip)
        {
            var button = ActionButton(caption, glyph, action);
            button.Width = width;
            button.MinWidth = width;
            button.Height = ToolbarHeight;
            button.MinHeight = ToolbarHeight;
            button.Padding = new Thickness(9, 0, 9, 0);
            button.Margin = new Thickness(0);
            button.ToolTip = tooltip;
            return button;
        }
        // Actions are enabled by actual filter state, never by the selected tab.
        // A collapsed clear button never consumes search-field space.
        private void UpdateToolbarActions()
        {
            var hasSearch = search != null && !String.IsNullOrEmpty(search.Text);
            if (searchClearButton != null)
                searchClearButton.Visibility = hasSearch ? Visibility.Visible : Visibility.Collapsed;
            if (searchPlaceholder != null)
                searchPlaceholder.Visibility = hasSearch ? Visibility.Collapsed : Visibility.Visible;
            if (toolbarResetButton != null)
                toolbarResetButton.IsEnabled = hasSearch ||
                    (platformFilter != null && platformFilter.SelectedIndex > 0) ||
                    (missingFilter != null && missingFilter.SelectedIndex > 0) || onlyIgnoredView;
        }
        private static Border Frame(Brush fill, Brush stroke, Thickness padding)
        {
            return new Border {Background = fill, BorderBrush = stroke,
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
                Padding = padding};
        }
        private static Brush GroupColor(WorkspaceTaskGroup group)
        {
            if (group == WorkspaceTaskGroup.Suggestions) return Accent;
            if (group == WorkspaceTaskGroup.NeedsChoice || group == WorkspaceTaskGroup.CheckNeeded ||
                group == WorkspaceTaskGroup.CouldNotCheck) return Warning;
            if (group == WorkspaceTaskGroup.Updated) return Good;
            if (group == WorkspaceTaskGroup.NeedsRecovery || group == WorkspaceTaskGroup.NotFound) return Bad;
            return Muted;
        }
        private static string GroupGlyph(WorkspaceTaskGroup group)
        {
            if (group == WorkspaceTaskGroup.Suggestions) return "\uE8F4";
            if (group == WorkspaceTaskGroup.Updated) return "\uE73E";
            if (group == WorkspaceTaskGroup.CheckNeeded) return "\uE72C";
            if (group == WorkspaceTaskGroup.NoNewInformation) return "\uE8D1";
            if (group == WorkspaceTaskGroup.Ignored) return "\uE711";
            if (group == WorkspaceTaskGroup.NeedsRecovery) return "\uE7BA";
            if (group == WorkspaceTaskGroup.NotFound) return "\uE711";
            return "\uE7BA";
        }
        private static Border Metric(string glyph, string title, Brush color, out TextBlock figure)
        {
            var card = Frame(Panel, SoftStroke, new Thickness(13, 9, 11, 9));
            card.Margin = new Thickness(0, 0, 9, 0);
            var content = new StackPanel();
            var line = new StackPanel {Orientation = Orientation.Horizontal};
            line.Children.Add(Symbol(glyph, color, 15));
            line.Children.Add(Label(title, 12, Muted));
            content.Children.Add(line);
            figure = Label("—", 23, color);
            figure.FontWeight = FontWeights.SemiBold;
            figure.Margin = new Thickness(0, 3, 0, 0);
            content.Children.Add(figure);
            card.Child = content;
            return card;
        }
        // One navigation system: no second Show dropdown competing with these tabs.
        private Button NavigationTab(string title, Brush accent, int index, out TextBlock number)
        {
            var line = new StackPanel {Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center};
            var label = Label(title, 14, Text);
            label.FontWeight = FontWeights.SemiBold;
            line.Children.Add(label);
            number = Label("—", 18, accent);
            number.FontWeight = FontWeights.SemiBold;
            number.Margin = new Thickness(9, 0, 0, 0);
            line.Children.Add(number);
            var button = new Button {Content = line, Background = Background,
                BorderBrush = SoftStroke, BorderThickness = new Thickness(0, 0, 0, 3),
                Padding = new Thickness(12, 10, 12, 9),
                Margin = new Thickness(0, 0, 6, 0), ToolTip = "Show " + title};
            button.Click += (sender, args) => scope.SelectedIndex = index;
            navigation.Add(button);
            return button;
        }
        private void UpdateNavigation()
        {
            int active = scope == null ? 0 : scope.SelectedIndex;
            for (int index = 0; index < navigation.Count; index++)
            {
                navigation[index].BorderBrush = index == active ? Accent : SoftStroke;
                navigation[index].Background = index == active ? PanelRaised : Background;
            }
        }
        private static Border ArtworkTile(Image image, string label, double width, double height)
        {
            var tile = Frame(Panel, SoftStroke, new Thickness(4));
            tile.Margin = new Thickness(0, 0, 6, 5);
            tile.Width = width + 10;
            var body = new StackPanel();
            image.Width = width; image.Height = height;
            image.Stretch = Stretch.Uniform;
            body.Children.Add(image);
            var name = Label(label, 10, Muted);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 4, 0, 0);
            body.Children.Add(name);
            tile.Child = body;
            return tile;
        }
        private static Border DetailSection(string glyph, string heading, Brush iconColor,
            TextBlock value)
        {
            var card = Frame(PanelRaised, SoftStroke, new Thickness(12, 11, 12, 11));
            card.Margin = new Thickness(0, 0, 0, 10);
            var content = new StackPanel();
            var headingRow = new StackPanel {Orientation = Orientation.Horizontal};
            headingRow.Children.Add(Symbol(glyph, iconColor, 14));
            var caption = Label(heading, 12, Muted);
            caption.FontWeight = FontWeights.SemiBold;
            headingRow.Children.Add(caption);
            content.Children.Add(headingRow);
            value.Margin = new Thickness(0, 8, 0, 0);
            content.Children.Add(value);
            card.Child = content;
            return card;
        }
        private static DataGridTemplateColumn StatusColumn()
        {
            var column = new DataGridTemplateColumn {Header = "Attention", IsReadOnly = true,
                SortMemberPath = "Status"};
            var template = new DataTemplate();
            var row = new FrameworkElementFactory(typeof(StackPanel));
            row.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            row.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            var icon = new FrameworkElementFactory(typeof(TextBlock));
            icon.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe MDL2 Assets"));
            icon.SetValue(TextBlock.FontSizeProperty, 13d);
            icon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            icon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 7, 0));
            icon.SetBinding(TextBlock.TextProperty, new Binding("StatusGlyph"));
            icon.SetBinding(TextBlock.ForegroundProperty, new Binding("StatusBrush"));
            row.AppendChild(icon);
            var caption = new FrameworkElementFactory(typeof(TextBlock));
            caption.SetValue(TextBlock.FontSizeProperty, 13d);
            caption.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            caption.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            caption.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            caption.SetBinding(TextBlock.TextProperty, new Binding("Status"));
            caption.SetBinding(TextBlock.ForegroundProperty, new Binding("StatusBrush"));
            row.AppendChild(caption);
            template.VisualTree = row;
            column.CellTemplate = template;
            return column;
        }
        private static string Name(WorkspaceTaskGroup group)
        {
            switch (group)
            {
                case WorkspaceTaskGroup.Suggestions: return "Suggested";
                case WorkspaceTaskGroup.NeedsChoice: return "Needs review";
                case WorkspaceTaskGroup.NotFound: return "No catalog match";
                case WorkspaceTaskGroup.CheckNeeded: return "To check";
                case WorkspaceTaskGroup.NoNewInformation: return "No missing fields";
                case WorkspaceTaskGroup.Updated: return "Already updated";
                case WorkspaceTaskGroup.Ignored: return "Ignored";
                case WorkspaceTaskGroup.CouldNotCheck: return "Lookup unsuccessful";
                case WorkspaceTaskGroup.NeedsRecovery: return "Recovery needs attention";
                default: return "Needs review";
            }
        }
        internal static string StatusGlyphFor(WorkspaceTaskGroup group) {return GroupGlyph(group);}
        internal static Brush StatusBrushFor(WorkspaceTaskGroup group) {return GroupColor(group);}
        private static bool InScope(WorkspaceTaskGroup group, int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0: return group == WorkspaceTaskGroup.Suggestions;
                case 1: return group == WorkspaceTaskGroup.NeedsChoice ||
                    group == WorkspaceTaskGroup.NotFound || group == WorkspaceTaskGroup.CouldNotCheck ||
                    group == WorkspaceTaskGroup.NeedsRecovery;
                case 2: return group == WorkspaceTaskGroup.CheckNeeded;
                case 3: return group == WorkspaceTaskGroup.Updated;
                case 4: return group == WorkspaceTaskGroup.NoNewInformation ||
                    group == WorkspaceTaskGroup.Ignored;
                default: return true;
            }
        }
        private static int MissingMask(Game game)
        {
            return CatalogEnrichmentNeeds.Mask(
                MetadataPlan.Missing(game.Description), MetadataPlan.Missing(game.CoverImage),
                MetadataPlan.Missing(game.BackgroundImage), MetadataPlan.Missing(game.Icon),
                !game.ReleaseDate.HasValue,
                game.GenreIds == null || game.GenreIds.Count == 0,
                game.DeveloperIds == null || game.DeveloperIds.Count == 0,
                game.PublisherIds == null || game.PublisherIds.Count == 0,
                game.SeriesIds == null || game.SeriesIds.Count == 0);
        }
        // Mirrors the classic scan's read-only identity inputs. The local
        // v0.5.18 cache may additionally invalidate on release year changes.
        private static CatalogInput ScanInput(Game game)
        {
            int mask = MissingMask(game);
            int localYear = 0;
            if (game.ReleaseDate.HasValue)
            {
                var text = game.ReleaseDate.Value.Serialize();
                var match = System.Text.RegularExpressions.Regex.Match(text ?? "", @"\b(?:19|20)\d{2}\b");
                if (match.Success) Int32.TryParse(match.Value, out localYear);
            }
            return new CatalogInput {Id = game.Id, Name = game.Name,
                Platform = game.Platforms == null ? "" : String.Join(", ", game.Platforms.Select(p => p.Name)),
                MissingMask = mask, MissingCount = CatalogEnrichmentNeeds.Count(mask),
                LocalReleaseYear = localYear};
        }
        private static bool JournalIsStillApplied(EnrichmentEntry entry, Game live)
        {
            return entry != null && live != null &&
                EnrichmentPlan.IsFullyApplied(entry, live.Name, live.SortingName,
                    live.Description, live.CoverImage, live.Icon, live.BackgroundImage) &&
                EnrichmentPlan.ExtraFieldsApplied(entry, GameExtrasSnapshot.ReleaseValue(live),
                    live.GenreIds, live.DeveloperIds, live.PublisherIds, live.SeriesIds);
        }
        private void ReadLocalLibrary()
        {
            // Read all required stores before publishing any counts. A malformed
            // journal/cache must not silently relabel 500 existing games as new.
            var scanCache = new CatalogScanCache(dataPath);
            scanCache.Load();
            var ignored = new CatalogIgnored(dataPath).Load();
            var confirmations = new CatalogConfirmedIdentities(dataPath);
            confirmations.Load();
            var journal = new EnrichmentJournal(dataPath);
            var completed = journal.Committed()
                .GroupBy(x => x.GameId).ToDictionary(x => x.Key, x => x.ToList());
            var recovery = CatalogRecoveryConsistency.Inspect(
                journal.MediaStaging.AllStages(), journal.AllEntries());
            bool recoveryBlocked = !recovery.AllowsWrites;
            var local = new List<LibraryWorkspaceItem>();
            var localGames = new Dictionary<Guid, Game>();
            var now = DateTime.UtcNow;
            foreach (var game in api.Database.Games.Where(g => g != null &&
                !String.IsNullOrWhiteSpace(g.Name) && !CatalogCore.IsTestingOnly(g.Name)))
            {
                localGames[game.Id] = game;
                var input = ScanInput(game);
                List<EnrichmentEntry> previous;
                bool applied = completed.TryGetValue(game.Id, out previous) &&
                    previous.Any(entry => JournalIsStillApplied(entry, game));
                CatalogScanRecord cached;
                bool saved = scanCache.TryGet(input, now, out cached);
                WorkspaceIdentityState identity = WorkspaceIdentityState.Unknown;
                string matchTitle = "Not checked";
                string evidence = "No reusable IGDB result for this game. It may be new or its metadata may have changed.";
                if (saved)
                {
                    matchTitle = String.IsNullOrWhiteSpace(cached.Candidate) ? "(no candidate)" : cached.Candidate;
                    evidence = "Saved IGDB result, not fresh approval evidence. No metadata will be changed here.";
                    long catalogId;
                    bool confirmed = Int64.TryParse(cached.CatalogId, out catalogId) && catalogId > 0 &&
                        confirmations.TryMatch(game.Id, input.Name, input.Platform, catalogId, cached.Candidate);
                    if (confirmed) identity = WorkspaceIdentityState.UserConfirmed;
                    else if (cached.Status == "Unmatched") identity = WorkspaceIdentityState.Unmatched;
                    else if (cached.Status == "Review") identity = WorkspaceIdentityState.NeedsReview;
                    else if (cached.Status == "Strong candidate") identity = WorkspaceIdentityState.Suggested;
                }
                var state = new WorkspaceTaskInput {
                    Lookup = saved ? WorkspaceLookupState.Saved : WorkspaceLookupState.NotChecked,
                    Identity = identity,
                    Fields = WorkspaceFieldsState.Potential,
                    Write = applied ? WorkspaceWriteState.Applied :
                        ignored.Contains(game.Id) ? WorkspaceWriteState.Ignored : WorkspaceWriteState.NotApplied,
                    HasSupportedMetadataGaps = input.MissingCount > 0
                };
                var decision = WorkspaceTaskClassifier.Classify(state);
                local.Add(new LibraryWorkspaceItem {
                    GameId = game.Id, Title = input.Name, Platform = input.Platform,
                    Source = game.Source == null ? "" : (game.Source.Name ?? "").Trim(),
                    Candidate = matchTitle, Group = decision.Group, Status = Name(decision.Group),
                    NextAction = decision.NextAction, Missing = CatalogEnrichmentNeeds.Describe(input.MissingMask),
                    Detail = evidence, FromSaved = saved,
                    CachedCatalogId = saved ? cached.CatalogId : null,
                    CachedCatalogName = saved ? cached.Candidate : null,
                    CachedMatchStatus = saved ? cached.Status : null,
                    CachedEvidence = saved ? cached.Evidence : null,
                    CachedScore = saved ? cached.Score : null,
                    CachedScannedUtc = saved ? cached.ScannedUtc : null,
                    MissingMask = input.MissingMask, IdentityConfirmed = identity == WorkspaceIdentityState.UserConfirmed,
                    SuggestedCoverId = saved ? cached.CoverId : null,
                    SuggestedScreenshotId = saved ? cached.ScreenshotId : null,
                    CachedFieldHints = saved ? cached.AvailableMetadata : null
                });
            }
            // Complete the read before publishing it; failed cache/journal loads
            // cannot silently replace valid state with an empty 'new games' queue.
            Guid keepFocus = table == null ? Guid.Empty :
                ((table.SelectedItem as LibraryWorkspaceItem) == null ? Guid.Empty :
                    ((LibraryWorkspaceItem)table.SelectedItem).GameId);
            knownGames.Clear();
            foreach (var pair in localGames) knownGames.Add(pair.Key, pair.Value);
            items.Clear();
            items.AddRange(local.OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase));
            // A refresh, identity correction, cache reset, or modified game snapshot
            // invalidates the earlier fresh evidence before another review can open.
            foreach (var key in freshStrictEvidence.Keys.ToList())
            {
                var savedRow=items.FirstOrDefault(x=>x.GameId==key);
                Game currentGame;
                if (savedRow==null || !knownGames.TryGetValue(key,out currentGame) ||
                    !FreshStrictReady(savedRow,ScanInput(currentGame)))
                    freshStrictEvidence.Remove(key);
            }
            markedIds.RemoveWhere(id => !knownGames.ContainsKey(id));
            bulkMarking = true;
            try
            {
                foreach (var item in items)
                {
                    item.MarkedChanged = OnMarkedChanged;
                    item.Marked = item.CanMark && markedIds.Contains(item.GameId);
                }
            }
            finally {bulkMarking = false;}
            markedIds.RemoveWhere(id => !items.Any(x => x.GameId == id && x.CanMark));
            if (recoveryBlocked)
            {
                notice.Text = "Import recovery records need attention. Native scanning can save lookup " +
                    "results, but imports are blocked. Inspect Advanced > Recovery ledgers " +
                    "(read-only); use Classic for legacy recovery if necessary.";
            }
            else
            {
                notice.Text = "Scans save IGDB suggestions only. Imports need an exact identity, checked " +
                    "fields, final approval and backup acknowledgment. Classic remains a fallback.";
            }
            int suggestions = items.Count(x => x.Group == WorkspaceTaskGroup.Suggestions);
            int choices = items.Count(x => InScope(x.Group, 1));
            int pending = items.Count(x => x.Group == WorkspaceTaskGroup.CheckNeeded);
            int updated = items.Count(x => x.Group == WorkspaceTaskGroup.Updated);
            int other = items.Count - suggestions - choices - pending - updated;
            totals.Text = items.Count + " games in Playnite · " + other +
                " in Other (no gaps, ignored and similar states)";
            metricSuggestions.Text = suggestions.ToString();
            metricChoices.Text = choices.ToString();
            metricPending.Text = pending.ToString();
            metricUpdated.Text = updated.ToString();
            metricOther.Text = other.ToString();
            metricAll.Text = items.Count.ToString();
            notice.Foreground = recoveryBlocked ? Warning : Muted;
            noticeFrame.BorderBrush = recoveryBlocked ? Warning : SoftStroke;
            // Rebuild platform options without firing partial-state filters mid-reload.
            var oldPlatform = platformFilter == null ? null : platformFilter.SelectedItem as string;
            if (platformFilter != null)
            {
                platformFilter.Items.Clear();
                platformFilter.Items.Add("All platforms");
                foreach (var platform in items.Select(x => x.Platform).Where(x => !String.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    platformFilter.Items.Add(platform);
                // Source is a display-only Playnite library facet, not a console
                // alias or IGDB match criterion. Keep it in the existing filter.
                foreach (var source in items.Select(x => x.Source).Where(x =>
                    !String.IsNullOrWhiteSpace(x) && x.Length + SourceFacetPrefix.Length <= 256)
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    platformFilter.Items.Add(SourceFacetPrefix + source);
                platformFilter.SelectedItem = oldPlatform != null &&
                    platformFilter.Items.Contains(oldPlatform) ? oldPlatform : "All platforms";
            }
            table.ItemsSource = null;
            table.ItemsSource = items;
            ApplyFilter();
            var focused = items.FirstOrDefault(x => x.GameId == keepFocus);
            if (focused != null && CollectionViewSource.GetDefaultView(table.ItemsSource).Contains(focused))
                table.SelectedItem = focused;
            UpdateSelectionSummary();
            UpdateScanControls();
        }
        private void OnMarkedChanged(LibraryWorkspaceItem item)
        {
            if (item.Marked && item.CanMark)
            {
                if (!markedIds.Contains(item.GameId) &&
                    !WorkspaceReviewHandoff.CanAddMarks(markedIds.Count, 1))
                {
                    // A checkbox must not create a selection the review cannot open.
                    // Reverting the checkbox triggers one ordinary unmark callback.
                    item.Marked = false;
                    if (scanStatus != null) scanStatus.Text =
                        "Review is limited to 1000 marked games at once. Unmark some first.";
                    return;
                }
                markedIds.Add(item.GameId);
            }
            else markedIds.Remove(item.GameId);
            if (!bulkMarking) {UpdateSelectionSummary(); UpdateScanControls();}
        }
        private List<LibraryWorkspaceItem> Highlighted()
        {
            return table == null ? new List<LibraryWorkspaceItem>() :
                table.SelectedItems.OfType<LibraryWorkspaceItem>().ToList();
        }
        private bool bulkMarking;
        private void MarkHighlighted(bool mark)
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying) return;
            var highlighted = Highlighted().Where(x => x.CanMark).ToList();
            int additions = mark ? highlighted.Count(x => !x.Marked) : 0;
            if (mark && !WorkspaceReviewHandoff.CanAddMarks(markedIds.Count, additions))
            {
                if (scanStatus != null) scanStatus.Text =
                    "Cannot mark highlighted games: review is limited to 1000 games. " +
                    "Unmark some games or narrow your selection first.";
                return;
            }
            bulkMarking = true;
            try {foreach (var item in highlighted) item.Marked = mark;}
            finally {bulkMarking = false; UpdateSelectionSummary(); UpdateScanControls();}
        }
        // A filtered view is an explicit, read-only selection scope, not import approval.
        // Hidden games and ineligible games are never added by this shortcut.
        private void MarkShown(bool mark)
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying ||
                table == null || table.ItemsSource == null) return;
            var shown = table.Items.Cast<object>().OfType<LibraryWorkspaceItem>()
                .Where(x => x.CanMark && x.Marked != mark).ToList();
            if (mark && !WorkspaceReviewHandoff.CanAddMarks(markedIds.Count, shown.Count))
            {
                if (scanStatus != null) scanStatus.Text =
                    "Cannot mark all shown: this would exceed the 1000-game review limit. " +
                    "Narrow the filter or unmark some games first; no marks changed.";
                return;
            }
            bulkMarking = true;
            try { foreach (var item in shown) item.Marked = mark; }
            finally {bulkMarking = false; UpdateSelectionSummary(); UpdateScanControls();}
            if (scanStatus != null) scanStatus.Text = (mark ? "Marked " : "Unmarked ") +
                shown.Count + " visible eligible game(s) for review only. No metadata changed.";
        }
        // Convenience for restoring ignored games without finding them among
        // Other/All entries. It is a view filter only, not a new persisted state.
        private void ShowIgnoredGames()
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying ||
                table == null || scope == null) return;
            if (search != null) search.Clear();
            if (platformFilter != null) platformFilter.SelectedIndex = 0;
            if (missingFilter != null) missingFilter.SelectedIndex = 0;
            scope.SelectedIndex = 5; // All games, narrowed to ignored status below.
            onlyIgnoredView = true;
            ApplyFilter();
            UpdateToolbarActions();
        }
        private void ClearMarks()
        {
            bulkMarking = true;
            try {foreach (var item in items.Where(x => x.Marked).ToList()) item.Marked = false;}
            finally {bulkMarking = false; UpdateSelectionSummary();}
            markedIds.Clear();
            UpdateSelectionSummary(); UpdateScanControls();
        }
        private void UpdateSelectionSummary()
        {
            if (selectionSummary == null) return;
            var highlightedRows = Highlighted();
            int highlighted = highlightedRows.Count;
            bool canMark = highlightedRows.Any(x => x.CanMark && !x.Marked);
            bool canUnmark = highlightedRows.Any(x => x.CanMark && x.Marked);
            selectionSummary.Text = highlighted + " highlighted  •  " + markedIds.Count +
                " marked for review (not import approval)";
            int visibleMarked = table == null || table.ItemsSource == null ? 0 :
                table.Items.Cast<object>().OfType<LibraryWorkspaceItem>().Count(x => x.Marked);
            if (markTraySummary != null)
                markTraySummary.Text = markedIds.Count + " marked" +
                    (markedIds.Count > visibleMarked ? " · " + (markedIds.Count - visibleMarked) +
                        " outside this view" : "") +
                    " · " + highlighted + " highlighted (Ctrl/Shift)";
            if (markHighlightedButton != null)
            {
                markHighlightedButton.IsEnabled = canMark;
                markHighlightedButton.Visibility = canMark ? Visibility.Visible : Visibility.Collapsed;
                unmarkHighlightedButton.IsEnabled = canUnmark;
                unmarkHighlightedButton.Visibility = canUnmark ? Visibility.Visible : Visibility.Collapsed;
                clearMarkedButton.IsEnabled = markedIds.Count > 0;
                clearMarkedButton.Visibility = markedIds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                bool hasMarks = markedIds.Count > 0;
                reviewMarksButton.IsEnabled = hasMarks;
                reviewMarksButton.Background = hasMarks ? Accent : Panel;
                reviewMarksButton.BorderBrush = hasMarks ? Accent : SoftStroke;
                reviewMarksButton.Content = IconLabel("\uE8A7", hasMarks ?
                    "Review " + markedIds.Count + " marked " + (markedIds.Count == 1 ? "game" : "games") :
                    "Review marked games", hasMarks ? Background : Muted);
            }
        }
        // Presentation-only facet. This never changes CatalogInput.Platform,
        // source trust, selections outside the view, or approved import plans.
        private static bool MatchesPlatformOrSource(LibraryWorkspaceItem row, string chosen)
        {
            if (row == null) return false;
            if (String.IsNullOrEmpty(chosen) || chosen == "All platforms") return true;
            if (chosen.StartsWith(SourceFacetPrefix, StringComparison.Ordinal))
                return String.Equals(row.Source,chosen.Substring(SourceFacetPrefix.Length),
                    StringComparison.OrdinalIgnoreCase);
            return String.Equals(row.Platform,chosen,StringComparison.OrdinalIgnoreCase);
        }
        private void ApplyFilter()
        {
            if (table == null || table.ItemsSource == null || scope == null || search == null) return;
            var view = CollectionViewSource.GetDefaultView(table.ItemsSource);
            if (view == null) return;
            var term = (search.Text ?? "").Trim();
            int selected = scope.SelectedIndex;
            var platform = platformFilter == null ? "All platforms" : platformFilter.SelectedItem as string;
            int field = missingFilter == null ? 0 : missingFilter.SelectedIndex;
            Guid focusedId = (table.SelectedItem as LibraryWorkspaceItem) == null ? Guid.Empty :
                ((LibraryWorkspaceItem)table.SelectedItem).GameId;
            view.Filter = obj => {
                var row = obj as LibraryWorkspaceItem;
                if (row == null || !InScope(row.Group, selected) ||
                    (onlyIgnoredView && row.Group != WorkspaceTaskGroup.Ignored)) return false;
                if (!MatchesPlatformOrSource(row, platform)) return false;
                if (field == 1 && (row.MissingMask & 14) == 0) return false; // artwork
                if (field == 2 && (row.MissingMask & 1) == 0) return false;  // description
                if (field == 3 && (row.MissingMask & ~15) == 0) return false; // other fields
                if (field == 4 && (row.MissingMask == 0 ||
                    !CatalogEnrichmentNeeds.VisibleInMissingScope(row.MissingMask, 1))) return false;
                if (field == 5 && !CatalogEnrichmentNeeds.VisibleInMissingScope(row.MissingMask, 2)) return false;
                return term.Length == 0 ||
                    ((row.Title ?? "") + " " + (row.Platform ?? "") + " " +
                     (row.Source ?? "") + " " + (row.Candidate ?? "") + " " + (row.Status ?? ""))
                        .IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            };
            view.Refresh();
            var visible = view.Cast<object>().OfType<LibraryWorkspaceItem>().ToList();
            var focused = visible.FirstOrDefault(x => x.GameId == focusedId);
            if (focused != null && !Highlighted().Contains(focused)) table.SelectedItem = focused;
            else if (table.SelectedItem == null || !view.Contains(table.SelectedItem))
                table.SelectedItem = visible.FirstOrDefault();
            if (listSummary != null)
                listSummary.Text = visible.Count + " shown of " + items.Count +
                    (onlyIgnoredView ? " — Ignored only; Reset or choose a scope to exit." : "") +
                    (visible.Count == 0 ? " — No matches. Clear search or filters." :
                    " — Ctrl/Shift to highlight; tick boxes to mark for preview review");
            UpdateSelectionSummary();
            UpdateNavigation();
            UpdateInspector();
        }
        private static BitmapSource LoadLocalArtwork(string path, int decodeWidth)
        {
            // Never fetch URL artwork during passive browsing. Some Playnite
            // fields may hold HTTPS URLs or inaccessible local paths.
            if (String.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) ||
                !File.Exists(path) || new FileInfo(path).Length > 24 * 1024 * 1024) return null;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = decodeWidth;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
        private bool ShowExisting(Image target, string imageId, int decodeWidth)
        {
            target.Source = null;
            if (String.IsNullOrWhiteSpace(imageId)) return false;
            try
            {
                var path = api.Database.GetFullFilePath(imageId);
                target.Source = LoadLocalArtwork(path, decodeWidth);
            }
            catch (Exception) {target.Source = null;}
            return target.Source != null;
        }
        private static bool ValidImageId(string id)
        {
            return !String.IsNullOrWhiteSpace(id) && id.Length <= 70 &&
                System.Text.RegularExpressions.Regex.IsMatch(id, @"^[A-Za-z0-9_]+$");
        }
        private void ClearSuggestedArtwork()
        {
            if (suggestedCover != null) suggestedCover.Source = null;
            if (suggestedScreenshot != null) suggestedScreenshot.Source = null;
        }
        private void LoadSuggestedArtwork()
        {
            var item = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            ClearSuggestedArtwork();
            if (item == null || !item.FromSaved) return;
            // Opt-in HTTPS image previews from saved IGDB image IDs only;
            // not new IGDB searches, identity confirmation or write evidence.
            try
            {
                if (ValidImageId(item.SuggestedCoverId))
                {
                    var b = new BitmapImage();
                    b.BeginInit(); b.DecodePixelWidth = 240;
                    b.CacheOption = BitmapCacheOption.OnDemand;
                    b.UriSource = new Uri("https://images.igdb.com/igdb/image/upload/t_cover_big/" +
                        item.SuggestedCoverId + ".jpg", UriKind.Absolute);
                    b.EndInit(); suggestedCover.Source = b;
                }
                if (ValidImageId(item.SuggestedScreenshotId))
                {
                    var b = new BitmapImage();
                    b.BeginInit(); b.DecodePixelWidth = 280;
                    b.CacheOption = BitmapCacheOption.OnDemand;
                    b.UriSource = new Uri("https://images.igdb.com/igdb/image/upload/t_screenshot_med/" +
                        item.SuggestedScreenshotId + ".jpg", UriKind.Absolute);
                    b.EndInit(); suggestedScreenshot.Source = b;
                }
                artworkNote.Text = "Suggested images come from a saved catalog candidate, not a verified match.";
            }
            catch (Exception ex)
            {
                ClearSuggestedArtwork();
                artworkNote.Text = "Suggested artwork is unavailable: " + ex.Message;
            }
        }
        // A saved lookup can hint at available imagery/summary, but only a live
        // exact-ID preflight can establish actual field additions. Never write
        // "2 to add" based solely on the locally cached candidate.
        private static List<WorkspaceFieldPreviewRow> FieldPreview(LibraryWorkspaceItem row)
        {
            var rows = new List<WorkspaceFieldPreviewRow>();
            if (row == null) return rows;
            var fields = new[] {
                new {Title="Description", Bit=1, Hint="description"},
                new {Title="Cover", Bit=2, Hint="cover"},
                new {Title="Background", Bit=4, Hint="screenshot"},
                new {Title="Icon", Bit=8, Hint="cover"},
                new {Title="Release date", Bit=16, Hint=""},
                new {Title="Genres", Bit=32, Hint=""},
                new {Title="Developers", Bit=64, Hint=""},
                new {Title="Publishers", Bit=128, Hint=""},
                new {Title="Series", Bit=256, Hint=""}
            };
            foreach (var field in fields)
            {
                bool missing = (row.MissingMask & field.Bit) != 0;
                if (!missing && field.Title != "Cover") continue;
                bool hinted = missing && field.Hint.Length != 0 &&
                    (" " + (row.CachedFieldHints ?? "").Replace(',', ' ').Replace(';', ' ') + " ")
                    .IndexOf(" " + field.Hint + " ", StringComparison.OrdinalIgnoreCase) >= 0;
                rows.Add(new WorkspaceFieldPreviewRow {
                    Field = field.Title,
                    Current = missing ? "Missing" : "Present",
                    Proposed = !missing ? "Keep existing" :
                        hinted ? "Candidate may have it · verify" : "Check live availability"
                });
            }
            rows.Add(new WorkspaceFieldPreviewRow {Field = "Display title", Current = row.Title,
                Proposed = "Keep existing · renaming is separate"});
            return rows;
        }
        private void MoveFocused(int offset)
        {
            if (table == null) return;
            var visible = table.Items.Cast<object>().OfType<LibraryWorkspaceItem>().ToList();
            if (visible.Count == 0) return;
            var focused = table.SelectedItem as LibraryWorkspaceItem;
            int at = visible.FindIndex(x => focused != null && x.GameId == focused.GameId);
            int next = Math.Max(0, Math.Min(visible.Count - 1, at + offset));
            // Navigation highlights; it never toggles an import mark.
            table.SelectedItem = visible[next];
            table.ScrollIntoView(visible[next]);
            table.Focus();
        }
        // Keep the user's place across the existing, separately approved review.
        // Only IDs, filters, and focus are captured; never identity trust or writes.
        private WorkspaceReturnState CaptureReturnState()
        {
            var focused = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            return new WorkspaceReturnState(markedIds, focused == null ? Guid.Empty : focused.GameId,
                scope == null ? 0 : Math.Max(0, scope.SelectedIndex),
                search == null ? "" : search.Text,
                platformFilter == null ? "All platforms" : platformFilter.SelectedItem as string,
                missingFilter == null ? 0 : Math.Max(0, missingFilter.SelectedIndex));
        }

        // Keep read-only information available for games without a usable saved
        // identity, and through the row context menu for eligible games.
        // Inspection never adds a review mark or requests IGDB data.
        private void ShowFocusedGameDetails()
        {
            var current = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            if (current == null) return;
            api.Dialogs.ShowMessage(
                current.Title + " (" + current.Platform + ")\n\n" +
                "Status: " + current.Status + "\nMissing metadata: " + current.Missing +
                "\nSuggested catalog title: " + current.Candidate + "\n\n" +
                current.NextAction + "\n" + current.Detail,
                "Game details — read-only");
        }
        private static bool NeedsManualIdentity(LibraryWorkspaceItem row)
        {
            long catalogId;
            return row != null && row.FromSaved && row.CanMark &&
                (!Int64.TryParse(row.CachedCatalogId, out catalogId) || catalogId <= 0 ||
                 String.IsNullOrWhiteSpace(row.CachedCatalogName));
        }
        private void ReviewFocusedGame()
        {
            if (BusyForNavigation() || table == null) return;
            var row = table.SelectedItem as LibraryWorkspaceItem;
            long igdbId;
            if (row == null || !row.CanMark || !row.FromSaved) return;
            // A no-match scan result needs an IGDB identity, not an import plan.
            // The search still requires exact-ID verification and explicit choice.
            if (NeedsManualIdentity(row))
            {
                if (ShowIdentitySearch(row.GameId, false)) ShowMarkedPreview(row.GameId);
                return;
            }
            if (!Int64.TryParse(row.CachedCatalogId, out igdbId) || igdbId <= 0 ||
                String.IsNullOrWhiteSpace(row.CachedCatalogName)) return;
            ShowMarkedPreview(row.GameId);
        }
        private void ShowMarkedPreview() { ShowMarkedPreview(null); }
        private void ShowMarkedPreview(Guid? focusedGameId)
        {
            if (BusyForNavigation()) return;
            WorkspaceReviewHandoff reviewScope;
            try { reviewScope = WorkspaceReviewHandoff.ForReviewScope(markedIds, focusedGameId); }
            catch (ArgumentException ex)
            {
                api.Dialogs.ShowMessage(ex.Message + " Unmark games or choose a smaller filtered set. " +
                    "No metadata was changed.", "Review selection unavailable");
                return;
            }
            var chosen = reviewScope.Filter(items, x => x.GameId)
                .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase).ToList();
            if (chosen.Count != reviewScope.Count || chosen.Any(x => !x.CanMark))
            {
                api.Dialogs.ShowMessage("A selected game is no longer eligible. Reload the library " +
                    "before reviewing; no metadata changed.", "Review selection changed");
                return;
            }
            // Resolve prerequisites before constructing a review that cannot proceed.
            // Other preview marks are unchanged; a corrected identity must still
            // receive an independent confirmation, live field check, and approval.
            var unscanned = chosen.FirstOrDefault(x => !x.FromSaved);
            if (unscanned != null)
            {
                api.Dialogs.ShowMessage("Scan " + unscanned.Title + " first using Scan pending " +
                    "in the workspace. The saved lookup is required before identity review. " +
                    "No metadata was changed.", "Scan required before review");
                return;
            }
            var unresolved = chosen.FirstOrDefault(NeedsManualIdentity);
            if (unresolved != null)
            {
                // Manual correction saves only an IGDB suggestion. Rebuild the
                // scoped plan from current library data after a successful choice.
                // A large marked batch may have several missing matches: never
                // cascade into an unbounded chain of dialogs.
                if (ShowIdentitySearch(unresolved.GameId, markedIds.Contains(unresolved.GameId)))
                {
                    int remaining = focusedGameId.HasValue ? 0 :
                        items.Count(x => markedIds.Contains(x.GameId) && NeedsManualIdentity(x));
                    if (remaining == 0) ShowMarkedPreview(focusedGameId);
                    else scanStatus.Text = "IGDB choice saved; " + remaining +
                        " more marked game(s) need a match. Choose Review marked games to continue.";
                }
                return;
            }
            // Native batch preview uses the EXISTING pure review planner. Never
            // infer strict evidence from restored scores or cached suggestions.
            List<WorkspaceBatchReviewRow> plan;
            try
            {
                var all = items.Select(x => {
                    Game live;
                    if (!knownGames.TryGetValue(x.GameId, out live) || live == null)
                        throw new InvalidOperationException("A Playnite game was removed. Reload before reviewing.");
                    var input = ScanInput(live);
                    return new WorkspaceBatchReviewInput {
                        GameId=x.GameId, GameName=input.Name, Platform=input.Platform,
                        MissingMask=input.MissingMask, MissingCount=input.MissingCount,
                        LocalReleaseYear=input.LocalReleaseYear, Marked=reviewScope.Includes(x.GameId),
                        Saved=x.FromSaved, Confirmed=x.IdentityConfirmed,
                        StrictEvidence=FreshStrictReady(x,input),
                        Completed=x.Group==WorkspaceTaskGroup.Updated,
                        Ignored=!x.CanMark && x.Group!=WorkspaceTaskGroup.Updated,
                        CandidateId=x.CachedCatalogId, CandidateTitle=x.CachedCatalogName,
                        MatchStatus=FreshStrictReady(x,input)?"Strong candidate":x.CachedMatchStatus,
                        DraftState=reviewScope.Includes(x.GameId)?CurrentDraft(x):null
                    };
                }).ToList();
                var library = knownGames.Values.Where(g=>g!=null).Select(g=>
                    Tuple.Create(g.Id,g.Name,g.Platforms==null?"":
                        String.Join(", ",g.Platforms.Select(p=>p.Name)))).ToList();
                plan = WorkspaceBatchReview.Preview(all, library);
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message, "Review selection needs attention");
                return;
            }
            var dialog = api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton = false});
            // One marked game and one focused game use the SAME compact plan.
            // The number of games, not the entry point, determines the layout;
            // exact GameId scope still comes exclusively from reviewScope.
            bool singleGameReview = chosen.Count == 1;
            dialog.Title = singleGameReview ? "Game — proposed review plan" :
                "Marked games — proposed review plan";
            dialog.Width = singleGameReview ? 920 : 1170;
            dialog.Height = singleGameReview ? 400 : 630;
            dialog.MinWidth = 740;
            dialog.MinHeight = singleGameReview ? 370 : 440;
            dialog.Owner = window; dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Background = Background;
            var root = new DockPanel {Margin = new Thickness(15)};
            var title = Label((singleGameReview ?
                "Review: " + chosen[0].Title :
                chosen.Count + " marked games") +
                " · Not yet approved", 19, Text);
            title.FontWeight = FontWeights.SemiBold;
            title.Margin = new Thickness(0, 0, 0, 8);
            DockPanel.SetDock(title, Dock.Top); root.Children.Add(title);
            // Lead with the next concrete step instead of a wall of technical
            // preflight instructions. All identity, plan and write gates remain.
            bool hasUnconfirmed = chosen.Any(x => !x.IdentityConfirmed);
            var reviewIntro = (hasUnconfirmed ?
                "1. Confirm uncertain identities.  2. Check exact fields.  3. Review changes and Apply." :
                "1. Check exact fields.  2. Review changes.  3. Apply.");
            var help = Label(reviewIntro + " Rows below are read-only summaries, not selections; " +
                "use the buttons below to continue. No data is imported until you explicitly approve the final plan " +
                "and acknowledge a restorable backup. Saved IGDB suggestions are not write evidence. " +
                "Classic is an optional fallback.", 13, Muted);
            help.Margin = new Thickness(0, 0, 0, 12);
            DockPanel.SetDock(help, Dock.Top); root.Children.Add(help);
            // Group prerequisites separately from the final decision. The
            // original button instances still own all enablement and callbacks.
            var actions = new StackPanel {Margin = new Thickness(0, 10, 0, 0)};
            var preparationActions = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Left};
            var decisionActions = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 7, 0, 0)};
            actions.Children.Add(preparationActions);
            actions.Children.Add(decisionActions);
            DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var closeReviewButton = ActionButton("Close review", "\uE8BB", () => dialog.Close());
            var liveStatus = Label(hasUnconfirmed ?
                "Next: Confirm uncertain IGDB identities, then check exact fields." :
                "Next: Check exact IGDB fields (read-only).", 12, Muted);
            liveStatus.Margin = new Thickness(0, 7, 0, 4);
            DockPanel.SetDock(liveStatus, Dock.Bottom);
            root.Children.Add(liveStatus);
            DataGrid grid = null;
            // A verified live field result belongs only to this modal review.
            // It cannot authorize imports and is discarded on a new check.
            var verifiedTargets = new Dictionary<Guid, WorkspaceLiveBatchTarget>();
            var verifiedDetails = new Dictionary<Guid,List<WorkspaceVerifiedFieldRow>>();
            var continueButton = ActionButton("Continue to Classic review", "\uE8A7", () => {
                WorkspaceReviewHandoff handoff;
                WorkspaceReviewContract verifiedReview;
                var transferDrafts = new List<CatalogImportDraft>();
                try
                {
                    if (!WorkspaceBatchLivePreflight.DetailedPlanReady(plan, verifiedTargets,
                        verifiedDetails))
                        throw new InvalidOperationException("Check exact IGDB fields and review the current title and field proposal for every marked game before continuing.");
                    verifiedReview = WorkspaceReviewContract.Capture(plan,verifiedTargets,verifiedDetails);
                    handoff = new WorkspaceReviewHandoff(chosen.Select(x => x.GameId));
                    foreach (var item in chosen)
                    {
                        WorkspaceLiveBatchTarget checkedGame = verifiedTargets[item.GameId];
                        var current = CurrentIdentityInput(item);
                        ConfirmUnchangedLookup(current,item.CachedCatalogId,item.CachedCatalogName);
                        WorkspaceBatchLivePreflight.EnsureCurrent(checkedGame,current,
                            checkedGame.IgdbId,checkedGame.CatalogTitle);
                        // Never silently lose a staged choice when leaving Design C.
                        bool hadDraft = draftStates.ContainsKey(item.GameId);
                        var saved = CurrentDraft(item);
                        if (hadDraft && saved == null)
                            throw new InvalidOperationException("A staged draft is stale. Recheck its exact identity and fields before transferring.");
                        if (!WorkspaceBatchLivePreflight.SameDraft(checkedGame.Draft,
                            saved == null ? null : saved.Draft))
                            throw new InvalidOperationException("A staged draft changed since its exact IGDB field check. Verify the batch again.");
                        if (saved == null) continue;
                        Game live;
                        long igdbId;
                        if (!knownGames.TryGetValue(item.GameId, out live) || live == null ||
                            !Int64.TryParse(item.CachedCatalogId, out igdbId) || igdbId <= 0 ||
                            !saved.Matches(ScanInput(live), igdbId, item.CachedCatalogName) ||
                            !CatalogImportDraft.Valid(saved.Draft))
                            throw new InvalidOperationException("Draft changed during review; recheck the current Playnite game and IGDB identity.");
                        transferDrafts.Add(saved.Draft.Copy());
                    }
                }
                catch (Exception ex)
                {
                    api.Dialogs.ShowMessage(ex.Message, "Review selection needs attention");
                    return;
                }
                if (BusyForNavigation())
                {
                    api.Dialogs.ShowMessage("Wait for the active check or import to finish " +
                        "before transferring selected games. Your review marks are unchanged.",
                        "Review transfer unavailable");
                    return;
                }
                var resume = CaptureReturnState();
                dialog.Close();
                window.Close();
                if (window.IsVisible) return; // Do not open Classic if Close was canceled.
                // The existing Classic session owns all matching, verification,
                // approval, backup and journal logic. When it closes, re-read the
                // live library before restoring remaining safe preview marks.
                try
                {
                    var classic = new CatalogWindow(api, dataPath, handoff);
                    // Recheck the exact reviewed Playnite snapshots on the
                    // receiving side. This is not a write or an import approval.
                    classic.StageVerifiedNativeReview(verifiedReview);
                    if (transferDrafts.Count > 0)
                        classic.StageTransferredDrafts(transferDrafts);
                    classic.Show();
                }
                catch (Exception ex)
                {
                    api.Dialogs.ShowErrorMessage("Selected review could not finish: " + ex.Message,
                        "Smart Metadata Matcher");
                }
                new LibraryWorkspacePreview(api, dataPath, resume).Show();
            });
            continueButton.IsEnabled = false;
            continueButton.ToolTip = "Transfer only after every selected game passes a fresh exact IGDB field check. " +
                "This does not authorize any metadata update.";
            // Native single-game confirmation remains unchanged. Native batches
            // may also use temporary fresh strict proof, with a new full resolver
            // check immediately before each journaled write.
            var nativeApplyButton = ActionButton("Apply this game...", "\uE8FB", () =>
                StartNativeSingleImport(chosen,plan,dialog,liveStatus,continueButton,
                    verifiedTargets,verifiedDetails), true);
            nativeApplyButton.IsEnabled=false;
            nativeApplyButton.ToolTip="Requires one confirmed identity, complete exact-field check, " +
                "itemized final approval and a restorable backup. Uses the original journaled writer.";
            var nativeBatchButton = ActionButton("Apply selected games...", "\uE8FB", () =>
                StartNativeConfirmedBatch(chosen,plan,dialog,liveStatus,continueButton,
                    verifiedTargets,verifiedDetails), true);
            nativeBatchButton.IsEnabled=false;
            nativeBatchButton.ToolTip="Requires two or more confirmed or freshly strict-verified IGDB identities, " +
                "complete exact-field verification, one itemized approval, and a restorable backup. " +
                "Sequential imports stop at the first error; completed games remain independently undoable.";
            Button detailsButton = null;
            detailsButton = ActionButton("View field-by-field plan", "\uE8A7", () => {
                try {ShowVerifiedBatchDetails(dialog,plan,verifiedTargets,verifiedDetails);}
                catch (Exception ex)
                {
                    verifiedTargets.Clear();verifiedDetails.Clear();
                    continueButton.IsEnabled=false;detailsButton.IsEnabled=false;
                    detailsButton.Visibility=Visibility.Collapsed;
                    nativeApplyButton.IsEnabled=false;nativeBatchButton.IsEnabled=false;
                    liveStatus.Text="Field plan invalidated. Recheck exact IGDB fields.";
                    api.Dialogs.ShowMessage("Field plan invalidated: " + ex.Message +
                        "\nNo metadata changed.","Field review unavailable");
                }
            });
            detailsButton.IsEnabled = false;
            detailsButton.Visibility = Visibility.Collapsed; // Not relevant before a complete field check.
            detailsButton.ToolTip = "View current Playnite values and IGDB field availability after every exact check succeeds. No import is approved here.";
            Button liveCheckButton = null;
            liveCheckButton = ActionButton("Check exact fields (read-only)", "\uE721", () =>
                CheckMarkedLiveFields(chosen, plan, grid, dialog, liveStatus,
                    continueButton, liveCheckButton, detailsButton, nativeApplyButton,
                    nativeBatchButton,verifiedTargets,verifiedDetails));
            Button confirmBatchIdentitiesButton = null;
            confirmBatchIdentitiesButton = ActionButton("Review unconfirmed identities...", "\uE8A7", () =>
                ConfirmMarkedIdentities(chosen,plan,dialog,grid,liveStatus,
                    continueButton,nativeApplyButton,nativeBatchButton,detailsButton,
                    liveCheckButton,confirmBatchIdentitiesButton,
                    verifiedTargets,verifiedDetails));
            confirmBatchIdentitiesButton.ToolTip = "Fetch and individually review each unconfirmed IGDB game, " +
                "platform and edition. Each identity requires its own explicit Yes. " +
                "Confirmation saves no Playnite metadata and never approves an import.";
            // One contextual Apply action per review. In particular, never show
            // a disabled batch-apply control when reviewing only one game.
            // Keep the same validation, dialogs, importer, and undo paths.
            confirmBatchIdentitiesButton.IsEnabled = hasUnconfirmed;
            confirmBatchIdentitiesButton.Visibility = hasUnconfirmed ? Visibility.Visible : Visibility.Collapsed;
            preparationActions.Children.Add(confirmBatchIdentitiesButton);
            preparationActions.Children.Add(liveCheckButton);
            preparationActions.Children.Add(detailsButton);
            decisionActions.Children.Add(chosen.Count == 1 ? nativeApplyButton : nativeBatchButton);
            decisionActions.Children.Add(continueButton);
            decisionActions.Children.Add(closeReviewButton);
            dialog.Closing += (sender, args) => {
                if (!checkingBatchFields && !nativeApplying) return;
                args.Cancel = true;
                if (nativeImportCancellation != null) nativeImportCancellation.Cancel();
                if (batchFieldCancellation != null) batchFieldCancellation.Cancel();
                liveStatus.Text = "Canceling exact IGDB field checks at the next safe boundary...";
            };
            grid = new DataGrid {AutoGenerateColumns = false, IsReadOnly = true,
                CanUserAddRows = false, CanUserDeleteRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                Background = Panel, Foreground = Text,
                ColumnHeaderHeight = 34, RowHeight = 31};
            ConfigureReadOnlyReviewGrid(grid);
            if (singleGameReview)
            {
                grid.MaxHeight = 105; // One row must not fill a large blank table.
                // No ten-column horizontal scroll for one game; all exact field
                // actions remain in the existing verified details dialog.
                AddColumn(grid, "Current title", "CurrentTitle", 200);
                AddColumn(grid, "Title after import (proposed)", "TargetTitle", 235);
                AddColumn(grid, "Identity", "Identity", 200);
                AddColumn(grid, "Next step", "NextStep", 245);
            }
            else
            {
                // Only task-driving columns belong in the batch overview.
                // Current title duplicates Game; Proposed title duplicates
                // TargetTitle; exact per-field availability has its own plan.
                // All truncated values retain their full-text tooltips.
                AddColumn(grid, "Game", "Game", 180);
                AddColumn(grid, "Platform", "Platform", 120);
                AddColumn(grid, "After import", "TargetTitle", 180);
                AddColumn(grid, "Identity", "Identity", 155);
                AddColumn(grid, "Draft", "Draft", 125);
                AddColumn(grid, "Next step", "NextStep", 180);
                AddColumn(grid, "Field check", "LiveCheck", 155);
            }
            grid.ItemsSource = plan;
            root.Children.Add(grid);
            dialog.Content = root; dialog.ShowDialog();
        }

        // Native multi-game import: either individually confirmed identities
        // OR current-session strict evidence. Each strict identity must also
        // pass a brand-new bounded resolver check at the write boundary.
        // No import begins until every marked game passes the exact field check,
        // the user sees the itemized full batch, and acknowledges a backup.
        // Native human-review path for saved suggestions. A cached Strong score
        // is NEVER converted into strict automatic import evidence. Only a
        // separate, exact-ID, per-game human Yes persists an identity decision.
        // This operation does not touch Playnite metadata or import journals.
        private void ConfirmMarkedIdentities(List<LibraryWorkspaceItem> chosen,
            List<WorkspaceBatchReviewRow> plan, Window dialog, DataGrid grid,
            TextBlock status, Button classicButton, Button singleButton,
            Button nativeBatchButton, Button detailsButton, Button checkButton,
            Button reviewButton,
            Dictionary<Guid,WorkspaceLiveBatchTarget> verifiedTargets,
            Dictionary<Guid,List<WorkspaceVerifiedFieldRow>> verifiedDetails)
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying ||
                chosen == null || plan == null || chosen.Count != plan.Count) return;
            var pending = chosen.Where(x => !x.IdentityConfirmed).ToList();
            if (pending.Count == 0)
            {
                status.Text = "All selected identities are already confirmed. Check exact fields before import.";
                return;
            }
            CatalogCredentialData credentials;
            var requests = new List<Tuple<LibraryWorkspaceItem,long,string>>();
            try
            {
                credentials = IdentityCredentials();
                foreach (var item in pending)
                {
                    long id;
                    var input = CurrentIdentityInput(item);
                    ConfirmUnchangedLookup(input,item.CachedCatalogId,item.CachedCatalogName);
                    if (!item.FromSaved || !Int64.TryParse(item.CachedCatalogId,out id) || id <= 0 ||
                        String.IsNullOrWhiteSpace(item.CachedCatalogName))
                        throw new InvalidOperationException("Resolve the saved IGDB suggestion for " +
                            item.Title + " before confirming it.");
                    requests.Add(Tuple.Create(item,id,item.CachedCatalogName));
                }
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message,"Batch identity review blocked");
                return;
            }
            // Identity changes invalidate ALL previous exact-field checks and
            // approval buttons. A fresh field comparison is mandatory afterward.
            verifiedTargets.Clear(); verifiedDetails.Clear();
            foreach (var row in plan)
            {
                row.LiveCheck="Not checked";
                row.AvailableFields="—";
            }
            grid.Items.Refresh();
            classicButton.IsEnabled=false;
            singleButton.IsEnabled=false;
            nativeBatchButton.IsEnabled=false;
            detailsButton.IsEnabled=false;
            detailsButton.Visibility=Visibility.Collapsed;
            checkButton.IsEnabled=false;
            reviewButton.IsEnabled=false;
            checkingBatchFields=true;
            batchFieldCancellation=new CancellationTokenSource();
            var cancellation=batchFieldCancellation;
            status.Text="Reviewing 0 / "+requests.Count+
                " exact IGDB identities. Each confirmation is separate; no metadata changes.";
            Task.Run(() => {
                int confirmed=0;
                string failure=null;
                bool declined=false;
                bool canceled=false;
                try
                {
                    var review=new WorkspaceIdentityReview(dataPath);
                    foreach (var entry in requests)
                    {
                        if (cancellation.IsCancellationRequested) {canceled=true;break;}
                        // Network calls never block the Playnite UI thread.
                        var exact=review.FetchExact(entry.Item2,credentials.ClientId,
                            credentials.ClientSecret,cancellation.Token);
                        dialog.Dispatcher.Invoke(new Action(() => {
                            cancellation.Token.ThrowIfCancellationRequested();
                            if (!dialog.IsVisible) throw new OperationCanceledException();
                            var item=entry.Item1;
                            var current=CurrentIdentityInput(item);
                            ConfirmUnchangedLookup(current,entry.Item2.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),entry.Item3);
                            long platformId=CatalogCore.PlatformId(current.Platform);
                            if (exact == null || exact.Id != entry.Item2 ||
                                !String.Equals(exact.Name,entry.Item3,StringComparison.Ordinal) ||
                                platformId <= 0 || exact.PlatformIds == null ||
                                !exact.PlatformIds.Contains(platformId))
                                throw new InvalidOperationException("Exact IGDB identity/title/platform " +
                                    "does not match "+item.Title+". Correct its match before importing.");
                            var answer=api.Dialogs.ShowMessage(
                                "Confirm this specific Playnite game's IGDB identity?\n\n"+
                                "Playnite: "+item.Title+" · "+current.Platform+"\n"+
                                "IGDB: "+exact.Name+" · ID "+exact.Id+"\n\n"+
                                "Check platform, sequel, edition and release carefully. " +
                                "This saves ONLY the identity; another exact field check, " +
                                "backup acknowledgement and final approval are still required.",
                                "Review IGDB identity ("+(confirmed+1)+" / "+requests.Count+")",
                                MessageBoxButton.YesNo);
                            if (answer != MessageBoxResult.Yes) {declined=true;return;}
                            cancellation.Token.ThrowIfCancellationRequested();
                            review.Confirm(current,item.CachedCatalogId,item.CachedCatalogName,exact);
                            item.IdentityConfirmed=true;
                            freshStrictEvidence.Remove(item.GameId);
                            var row=plan.Single(x=>x.GameId==item.GameId);
                            row.Identity="User-confirmed · recheck exact ID";
                            if (row.NextStep != "Resolve title collision" &&
                                row.NextStep != "Recheck draft" &&
                                !String.IsNullOrWhiteSpace(row.TargetTitle))
                            {
                                row.ReadyForPreflight=true;
                                row.NextStep="Eligible for live preflight · not approved";
                            }
                            confirmed++;
                            grid.Items.Refresh();
                            status.Text="Confirmed "+confirmed+" / "+requests.Count+
                                ". No import permission granted; exact fields still need checking.";
                        }));
                        if (declined) break;
                    }
                }
                catch (OperationCanceledException) {canceled=true;}
                catch (Exception ex) {failure=ex.Message;}
                var error=failure;
                var wasCanceled=canceled;
                var wasDeclined=declined;
                var saved=confirmed;
                dialog.Dispatcher.BeginInvoke(new Action(() => {
                    checkingBatchFields=false;
                    if (batchFieldCancellation==cancellation)
                    {
                        batchFieldCancellation.Dispose();
                        batchFieldCancellation=null;
                    }
                    if (!dialog.IsVisible) return;
                    checkButton.IsEnabled=true;
                    reviewButton.IsEnabled=chosen.Any(x=>!x.IdentityConfirmed);
                    reviewButton.Visibility=reviewButton.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
                    status.Text=error!=null?"Identity review stopped: "+error:
                        wasCanceled?"Identity review canceled after "+saved+" confirmation(s).":
                        wasDeclined?"Review paused by you after "+saved+" confirmation(s).":
                        "Confirmed "+saved+" game(s). Run Check exact fields for the complete batch.";
                    if (error!=null)
                        api.Dialogs.ShowMessage(error+"\nNo metadata import was started.",
                            "Identity review needs attention");
                }));
            });
        }

        private void StartNativeConfirmedBatch(List<LibraryWorkspaceItem> chosen,
            List<WorkspaceBatchReviewRow> plan, Window reviewOwner, TextBlock status,
            Button classicButton,
            Dictionary<Guid,WorkspaceLiveBatchTarget> verifiedTargets,
            Dictionary<Guid,List<WorkspaceVerifiedFieldRow>> verifiedDetails)
        {
            if (nativeApplying || checkingBatchFields || checkingLiveFields || scanning ||
                chosen == null || plan == null || chosen.Count < 2 ||
                chosen.Count != plan.Count)
                return;
            WorkspaceReviewContract frozen;
            CatalogCredentialData credentials;
            var approved = new List<WorkspaceNativeBatchEntry>();
            try
            {
                if (!chosen.All(x=>NativeBatchReady(x)))
                    throw new InvalidOperationException("Every selected game needs current human confirmation or fresh strict evidence.");
                if (!WorkspaceBatchLivePreflight.DetailedPlanReady(plan,verifiedTargets,verifiedDetails))
                    throw new InvalidOperationException("Check exact IGDB fields for every selected game first.");
                frozen=WorkspaceReviewContract.Capture(plan,verifiedTargets,verifiedDetails);
                var byId=plan.ToDictionary(x=>x.GameId);
                var confirmed=new CatalogConfirmedIdentities(dataPath);
                confirmed.Load();
                foreach (var selected in chosen)
                {
                    var proposed=byId[selected.GameId];
                    var target=verifiedTargets[selected.GameId];
                    var current=CurrentIdentityInput(selected);
                    ConfirmUnchangedLookup(current,selected.CachedCatalogId,selected.CachedCatalogName);
                    WorkspaceBatchLivePreflight.EnsureCurrent(target,current,
                        target.IgdbId,target.CatalogTitle);
                    bool hadDraft=draftStates.ContainsKey(selected.GameId);
                    var actualDraft=CurrentDraft(selected);
                    if ((hadDraft && actualDraft==null) ||
                        !WorkspaceBatchLivePreflight.SameDraft(target.Draft,
                            actualDraft==null?null:actualDraft.Draft))
                        throw new InvalidOperationException("Staged draft changed for "+selected.Title+". Recheck the batch.");
                    bool strict = !selected.IdentityConfirmed && FreshStrictReady(selected,current);
                    if (!strict && !confirmed.TryMatch(current.Id,current.Name,current.Platform,
                        target.IgdbId,target.CatalogTitle))
                        throw new InvalidOperationException("IGDB identity has no current confirmation or strict evidence for "+selected.Title+".");
                    approved.Add(new WorkspaceNativeBatchEntry {
                        FreshStrict=strict,
                        GameId=selected.GameId, IgdbId=target.IgdbId,
                        CatalogTitle=target.CatalogTitle, ApprovedTitle=proposed.TargetTitle,
                        Draft=target.Draft==null?null:target.Draft.Copy(),
                        Snapshot=WorkspaceNativeApprovedSnapshot.Capture(api.Database.Games[selected.GameId])
                    });
                }
                frozen.EnsureSamePlan(approved.Select(x=>Tuple.Create(x.GameId,x.IgdbId,
                    x.CatalogTitle,x.ApprovedTitle,x.Draft)));
                frozen.EnsureCurrent(id => {
                    var latest=api.Database.Games[id];
                    return latest==null?null:ScanInput(latest);
                });
                credentials=IdentityCredentials();
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message,"Native batch approval blocked");
                return;
            }
            var confirm=api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton=false});
            confirm.Title="Final Design C import approval — verified batch";
            confirm.Width=1240;confirm.Height=760;
            confirm.MinWidth=770;confirm.MinHeight=480;
            confirm.Owner=reviewOwner;
            confirm.WindowStartupLocation=WindowStartupLocation.CenterOwner;
            confirm.Background=Background;
            var body=new DockPanel {Margin=new Thickness(16)};
            var heading=new StackPanel {Margin=new Thickness(0,0,0,12)};
            DockPanel.SetDock(heading,Dock.Top);body.Children.Add(heading);
            heading.Children.Add(Label("Approve "+approved.Count+" verified Playnite metadata updates",19,Text));
            heading.Children.Add(Label("Each game has a confirmed identity or independently verified fresh strict match. " +
                "Review all exact IDs, Playnite titles after import, and field actions below. " +
                "Only missing metadata is added unless a custom description was staged. " +
                "No ROM files, installation paths, launch actions, or custom sorting are changed.",12,Muted));
            var footer=new StackPanel {Margin=new Thickness(0,10,0,0)};
            DockPanel.SetDock(footer,Dock.Bottom);body.Children.Add(footer);
            footer.Children.Add(Label("Sequential writes stop at the first error or cancellation. " +
                "Previously completed games remain applied with individual journal/undo records. " +
                "This is NOT an atomic all-or-nothing batch.",12,Warning));
            var backup=new CheckBox {Content="I verified a restorable backup of my Playnite library AND extension data.",
                IsChecked=false,Foreground=Text,Margin=new Thickness(0,9,0,9)};
            footer.Children.Add(backup);
            var buttons=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right};
            footer.Children.Add(buttons);
            var apply=ActionButton("Apply "+approved.Count+" verified games","\uE8FB",
                ()=>confirm.DialogResult=true,true);
            apply.IsEnabled=false;
            backup.Checked+=(s,e)=>apply.IsEnabled=true;
            backup.Unchecked+=(s,e)=>apply.IsEnabled=false;
            buttons.Children.Add(ActionButton("Cancel — no changes","\uE8BB",()=>confirm.DialogResult=false));
            buttons.Children.Add(apply);
            var fields=new DataGrid {AutoGenerateColumns=false,IsReadOnly=true,
                CanUserAddRows=false,CanUserDeleteRows=false,
                Background=Panel,Foreground=Text,RowBackground=Panel,
                AlternatingRowBackground=AlternateRow,AlternationCount=2,
                HeadersVisibility=DataGridHeadersVisibility.Column,
                EnableRowVirtualization=true,ColumnHeaderHeight=34,RowHeight=32};
            AddColumn(fields,"Playnite game","Game",190);
            AddColumn(fields,"Platform","Platform",125);
            AddColumn(fields,"IGDB ID","IgdbId",85);
            AddColumn(fields,"Field","Field",130);
            AddColumn(fields,"Current Playnite value","Current",215);
            AddColumn(fields,"Verified IGDB availability","Igdb",235);
            AddColumn(fields,"Proposed handling","PlannedAction",235);
            fields.ItemsSource=plan.SelectMany(x=>verifiedDetails[x.GameId]).ToList();
            body.Children.Add(fields);confirm.Content=body;
            if (confirm.ShowDialog()!=true) return;
            try
            {
                frozen.EnsureCurrent(id=>{
                    var latest=api.Database.Games[id];
                    return latest==null?null:ScanInput(latest);
                });
                frozen.EnsureSamePlan(approved.Select(x=>Tuple.Create(x.GameId,x.IgdbId,
                    x.CatalogTitle,x.ApprovedTitle,x.Draft)));
                var againConfirmed=new CatalogConfirmedIdentities(dataPath);
                againConfirmed.Load();
                foreach (var entry in approved)
                {
                    var selected=chosen.Single(x=>x.GameId==entry.GameId);
                    var current=CurrentIdentityInput(selected);
                    ConfirmUnchangedLookup(current,selected.CachedCatalogId,selected.CachedCatalogName);
                    if (entry.FreshStrict)
                    {
                        if (selected.IdentityConfirmed || !FreshStrictReady(selected,current))
                            throw new InvalidOperationException("Fresh strict evidence expired or changed for "+entry.Snapshot.Name+".");
                    }
                    else if (!selected.IdentityConfirmed ||
                        !againConfirmed.TryMatch(current.Id,current.Name,current.Platform,
                            entry.IgdbId,entry.CatalogTitle))
                        throw new InvalidOperationException("Human-confirmed identity expired or changed for "+entry.Snapshot.Name+".");
                    if (!entry.Snapshot.Matches(api.Database.Games[entry.GameId]))
                        throw new InvalidOperationException("Playnite metadata changed during approval for "+entry.Snapshot.Name+".");
                    var draft=CurrentDraft(selected);
                    if ((draftStates.ContainsKey(selected.GameId) && draft==null) ||
                        !WorkspaceBatchLivePreflight.SameDraft(entry.Draft,
                            draft==null?null:draft.Draft))
                        throw new InvalidOperationException("A batch draft changed after final review.");
                }
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message,"Approval invalidated — no import started");
                return;
            }
            nativeApplying=true;
            nativeImportCancellation=new CancellationTokenSource();
            var cancellation=nativeImportCancellation;
            classicButton.IsEnabled=false;
            status.Text="Native import: 0 / "+approved.Count+" verified games processed...";
            Task.Run(()=>{
                WorkspaceNativeBatchResult result;
                try
                {
                    result=WorkspaceNativeConfirmedBatch.Execute(api,dataPath,frozen,ScanInput,
                        reviewOwner.Dispatcher,approved,credentials.ClientId,credentials.ClientSecret,
                        cancellation.Token,(completed,total,appliedCount,description)=>
                            reviewOwner.Dispatcher.BeginInvoke(new Action(()=>{
                                if (reviewOwner.IsVisible)
                                    status.Text="Native import: "+completed+" / "+total+
                                        " processed ("+appliedCount+" updated). "+description;
                            })));
                }
                catch (Exception ex)
                {
                    result=WorkspaceNativeBatchResult.Fatal(approved.Count,ex.Message);
                }
                reviewOwner.Dispatcher.BeginInvoke(new Action(()=>{
                    nativeApplying=false;
                    if (nativeImportCancellation != null)
                    {
                        nativeImportCancellation.Dispose();
                        nativeImportCancellation=null;
                    }
                    verifiedTargets.Clear();verifiedDetails.Clear();
                    classicButton.IsEnabled=false;
                    foreach(var done in result.ProcessedGameIds)markedIds.Remove(done);
                    if (!reviewOwner.IsVisible) return;
                    var summary="Native batch: "+result.Applied+" applied, "+
                        result.Skipped+" no changes, "+result.Failed+" failed, "+
                        (result.Total-result.ProcessedGameIds.Count-result.Failed)+" not attempted."+
                        (result.Canceled?" Canceled at a safe boundary.":"")+
                        (result.Failure==null?"":" Stopped: "+result.Failure)+
                        " Individual completed updates remain in History/Undo.";
                    status.Text=summary;
                    if (result.ProcessedGameIds.Count>0)
                    {
                        reviewOwner.Close();
                        SafeReload();
                    }
                    if (result.Failure!=null)
                        api.Dialogs.ShowErrorMessage(summary+"\nInspect History/Recovery before another import.",
                            "Native batch stopped");
                    else api.Dialogs.ShowMessage(summary,"Native batch result");
                }));
            });
        }

        // Native single-game final approval, deliberately distinct from merely
        // marking a row or checking IGDB field availability.
        private void StartNativeSingleImport(List<LibraryWorkspaceItem> chosen,
            List<WorkspaceBatchReviewRow> plan, Window reviewOwner, TextBlock status,
            Button classicButton,
            Dictionary<Guid,WorkspaceLiveBatchTarget> verifiedTargets,
            Dictionary<Guid,List<WorkspaceVerifiedFieldRow>> verifiedDetails)
        {
            if (nativeApplying || checkingBatchFields || chosen.Count!=1 || plan.Count!=1)
                return;
            LibraryWorkspaceItem selected=chosen[0];
            if (!selected.IdentityConfirmed)
            {
                api.Dialogs.ShowMessage("Confirm this game's exact IGDB identity and edition in the inspector first.",
                    "Native import requires confirmed identity");
                return;
            }
            WorkspaceReviewContract frozen;
            WorkspaceNativeApprovedSnapshot approvedSnapshot;
            WorkspaceLiveBatchTarget target;
            WorkspaceBatchReviewRow proposed=plan[0];
            CatalogCredentialData credentials;
            try
            {
                if (!WorkspaceBatchLivePreflight.DetailedPlanReady(plan,verifiedTargets,verifiedDetails))
                    throw new InvalidOperationException("Recheck exact IGDB fields before applying.");
                target=verifiedTargets[selected.GameId];
                var current=CurrentIdentityInput(selected);
                ConfirmUnchangedLookup(current,selected.CachedCatalogId,selected.CachedCatalogName);
                WorkspaceBatchLivePreflight.EnsureCurrent(target,current,
                    target.IgdbId,target.CatalogTitle);
                var actualDraft=CurrentDraft(selected);
                if ((draftStates.ContainsKey(selected.GameId) && actualDraft==null) ||
                    !WorkspaceBatchLivePreflight.SameDraft(target.Draft,
                        actualDraft==null?null:actualDraft.Draft))
                    throw new InvalidOperationException("The staged draft changed since the field check.");
                var identities=new CatalogConfirmedIdentities(dataPath);
                identities.Load();
                if (!identities.TryMatch(current.Id,current.Name,current.Platform,
                    target.IgdbId,target.CatalogTitle))
                    throw new InvalidOperationException("The confirmed IGDB identity is no longer valid.");
                frozen=WorkspaceReviewContract.Capture(plan,verifiedTargets,verifiedDetails);
                frozen.EnsureSamePlan(new[] {Tuple.Create(selected.GameId,target.IgdbId,
                    target.CatalogTitle,proposed.TargetTitle,target.Draft)});
                frozen.EnsureCurrent(id => {
                    var latest=api.Database.Games[id];
                    return latest==null?null:ScanInput(latest);
                });
                approvedSnapshot=WorkspaceNativeApprovedSnapshot.Capture(
                    api.Database.Games[selected.GameId]);
                credentials=IdentityCredentials();
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message,"Native import review blocked");
                return;
            }
            // Itemized final approval: no checkbox for disabling standard title
            // cleanup. Only Playnite's display Name may change, never ROM paths.
            var confirm=api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton=false});
            confirm.Title="Final Design C import approval — one game";
            confirm.Width=1110;confirm.Height=670;
            confirm.MinWidth=700;confirm.MinHeight=450;
            confirm.Owner=reviewOwner;
            confirm.WindowStartupLocation=WindowStartupLocation.CenterOwner;
            confirm.Background=Background;
            var body=new DockPanel {Margin=new Thickness(16)};
            var heading=new StackPanel {Margin=new Thickness(0,0,0,12)};
            DockPanel.SetDock(heading,Dock.Top);body.Children.Add(heading);
            heading.Children.Add(Label("Approve ONE Playnite metadata update",19,Text));
            heading.Children.Add(Label("Game: "+target.GameName+"  ·  "+target.Platform+
                "  ·  exact IGDB ID "+target.IgdbId,13,Muted));
            heading.Children.Add(Label("Display title after import: "+proposed.TargetTitle+
                ". Other populated fields are preserved unless you explicitly staged a custom description. "+
                "Final availability may depend on artwork downloads. ROM files, paths, executables, "+
                "sorting and launch actions are untouched.",12,Muted));
            var footer=new StackPanel {Margin=new Thickness(0,10,0,0)};
            DockPanel.SetDock(footer,Dock.Bottom);body.Children.Add(footer);
            var backup=new CheckBox {Content="I verified a restorable backup of my Playnite library AND extension data.",
                IsChecked=false,Foreground=Text,Margin=new Thickness(0,0,0,9)};
            footer.Children.Add(backup);
            var buttons=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right};
            footer.Children.Add(buttons);
            var approve=ActionButton("Apply this game","\uE8FB",()=>confirm.DialogResult=true,true);
            approve.IsEnabled=false;
            backup.Checked+=(s,e)=>approve.IsEnabled=true;
            backup.Unchecked+=(s,e)=>approve.IsEnabled=false;
            buttons.Children.Add(ActionButton("Cancel — no changes","\uE8BB",()=>confirm.DialogResult=false));
            buttons.Children.Add(approve);
            var fields=new DataGrid {AutoGenerateColumns=false,IsReadOnly=true,
                CanUserAddRows=false,CanUserDeleteRows=false,
                Background=Panel,Foreground=Text,RowBackground=Panel,
                AlternatingRowBackground=AlternateRow,AlternationCount=2,
                HeadersVisibility=DataGridHeadersVisibility.Column,
                EnableRowVirtualization=true,ColumnHeaderHeight=34,RowHeight=32};
            AddColumn(fields,"Field","Field",130);
            AddColumn(fields,"Current Playnite value","Current",245);
            AddColumn(fields,"Exact IGDB result","Igdb",245);
            AddColumn(fields,"Proposed handling","PlannedAction",280);
            fields.ItemsSource=verifiedDetails[selected.GameId].ToList();
            body.Children.Add(fields);confirm.Content=body;
            if (confirm.ShowDialog()!=true) return;
            // Recheck after the modal; user could have changed the underlying
            // Playnite record using another interface while reviewing.
            try
            {
                frozen.EnsureCurrent(id => {
                    var latest=api.Database.Games[id];
                    return latest==null?null:ScanInput(latest);
                });
                var identities=new CatalogConfirmedIdentities(dataPath);
                identities.Load();
                var current=CurrentIdentityInput(selected);
                if (!identities.TryMatch(current.Id,current.Name,current.Platform,
                    target.IgdbId,target.CatalogTitle))
                    throw new InvalidOperationException("Confirmed identity changed during approval.");
                ConfirmUnchangedLookup(current,selected.CachedCatalogId,selected.CachedCatalogName);
                if (!approvedSnapshot.Matches(api.Database.Games[selected.GameId]))
                    throw new InvalidOperationException("Playnite metadata changed during final approval.");
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message,"Approval invalidated — no import started");
                return;
            }
            nativeApplying=true;
            nativeImportCancellation=new CancellationTokenSource();
            var token=nativeImportCancellation.Token;
            classicButton.IsEnabled=false;
            status.Text="Fetching exact IGDB identity and artwork for one approved game...";
            var frozenDraft=target.Draft==null?null:target.Draft.Copy();
            Task.Run(() => {
                CatalogApplyResult applied=null;
                string failure=null;
                bool cancelled=false;
                try
                {
                    applied=WorkspaceNativeSingleImport.Execute(api,dataPath,frozen,ScanInput,
                        approvedSnapshot,reviewOwner.Dispatcher,selected.GameId,target.IgdbId,target.CatalogTitle,
                        proposed.TargetTitle,frozenDraft,credentials.ClientId,credentials.ClientSecret,token);
                }
                catch (OperationCanceledException) {cancelled=true;}
                catch (Exception ex) {failure=ex.Message;}
                var outcome=applied;
                var error=failure;
                var wasCancelled=cancelled;
                reviewOwner.Dispatcher.BeginInvoke(new Action(() => {
                    nativeApplying=false;
                    if (nativeImportCancellation != null)
                    {
                        nativeImportCancellation.Dispose();
                        nativeImportCancellation=null;
                    }
                    // A completed, failed, or canceled attempt requires a NEW
                    // live field check; never reuse this approval or field plan.
                    verifiedTargets.Clear();verifiedDetails.Clear();
                    classicButton.IsEnabled=false;
                    if (!reviewOwner.IsVisible) return;
                    if (error != null)
                    {
                        status.Text="Native import stopped: "+error;
                        api.Dialogs.ShowErrorMessage(error+"\nInspect History/Recovery before any retry.",
                            "Native metadata import stopped");
                    }
                    else if (wasCancelled)
                    {
                        status.Text="Native import canceled before the next Playnite write.";
                    }
                    else
                    {
                        status.Text=outcome!=null && outcome.NoChanges?
                            "No safe new metadata fields available; no write recorded.":
                            "One game imported through the shared journaled writer.";
                        reviewOwner.Close();
                        SafeReload();
                        api.Dialogs.ShowMessage(status.Text,"Smart Metadata Matcher");
                    }
                }));
            });
        }

        // Inspect the exact-record field comparison without claiming approval.
        // This is a read-only proposal; Classic independently checks and asks
        // for final confirmation before any Playnite metadata write.
        private void ShowVerifiedBatchDetails(Window owner,
            List<WorkspaceBatchReviewRow> plan,
            Dictionary<Guid,WorkspaceLiveBatchTarget> verifiedTargets,
            Dictionary<Guid,List<WorkspaceVerifiedFieldRow>> verifiedDetails)
        {
            if (!WorkspaceBatchLivePreflight.DetailedPlanReady(plan,verifiedTargets,
                verifiedDetails))
            {
                api.Dialogs.ShowMessage("The live field plan is incomplete or stale. " +
                    "Check every selected game again.", "Field review unavailable");
                return;
            }
            foreach (var row in plan)
            {
                var target=verifiedTargets[row.GameId];
                var game=CurrentIdentityInput(items.Single(x=>x.GameId==row.GameId));
                ConfirmUnchangedLookup(game,target.IgdbId.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),target.CatalogTitle);
                WorkspaceBatchLivePreflight.EnsureCurrent(target,game,
                    target.IgdbId,target.CatalogTitle);
                var item=items.Single(x=>x.GameId==row.GameId);
                bool hadDraft=draftStates.ContainsKey(row.GameId);
                var currentDraft=CurrentDraft(item);
                if ((hadDraft && currentDraft==null) ||
                    !WorkspaceBatchLivePreflight.SameDraft(target.Draft,
                        currentDraft==null?null:currentDraft.Draft))
                    throw new InvalidOperationException("Draft changed after verification. Recheck exact fields.");
            }
            var review=api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton=false});
            review.Title="Verified IGDB field proposals — read-only";
            review.Width=1210;review.Height=690;
            review.MinWidth=760;review.MinHeight=420;
            review.Owner=owner;review.WindowStartupLocation=WindowStartupLocation.CenterOwner;
            review.Background=Background;
            var root=new DockPanel {Margin=new Thickness(15)};
            var heading=Label("Verified field availability · " + plan.Count +
                " selected games",19,Text);
            heading.FontWeight=FontWeights.SemiBold;
            heading.Margin=new Thickness(0,0,0,7);
            DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
            var note=Label("Comparison only: rows cannot be selected or edited. Existing fields " +
                "stay unchanged unless an approved draft permits a change. No metadata was imported. " +
                "Next: Back to review, then choose Apply for the final itemized approval " +
                "and backup check; Classic remains a fallback.",12,Muted);
            note.Margin=new Thickness(0,0,0,12);
            DockPanel.SetDock(note,Dock.Top);root.Children.Add(note);
            var close=ActionButton("Back to review","\uE8BB",()=>review.Close());
            close.ToolTip="Final approval independently rechecks the identity, current Playnite fields, " +
                "backup acknowledgment and title safety. This read-only window cannot approve an import. " +
                "Classic is available as a fallback.";
            var footer=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right,
                Margin=new Thickness(0,12,0,0)};
            footer.Children.Add(close);
            DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
            var grid=new DataGrid {AutoGenerateColumns=false,IsReadOnly=true,
                CanUserAddRows=false,CanUserDeleteRows=false,
                HeadersVisibility=DataGridHeadersVisibility.Column,
                Background=Panel,Foreground=Text,
                ColumnHeaderHeight=34,RowHeight=32,
                EnableRowVirtualization=true};
            ConfigureReadOnlyReviewGrid(grid);
            AddColumn(grid,"Playnite game","Game",195);
            AddColumn(grid,"Platform","Platform",140);
            AddColumn(grid,"IGDB ID","IgdbId",84);
            AddColumn(grid,"Field","Field",130);
            AddColumn(grid,"Current Playnite value","Current",245);
            AddColumn(grid,"IGDB result / availability","Igdb",240);
            AddColumn(grid,"Proposed handling","PlannedAction",250);
            grid.ItemsSource=plan.SelectMany(x=>verifiedDetails[x.GameId]).ToList();
            root.Children.Add(grid);
            review.Content=root;review.ShowDialog();
        }

        // Inspect marked exact IGDB records without approving or writing metadata.
        // Snapshots are checked both before and after network I/O. The original
        // Classic preflight and journaled writer remain authoritative.
        private void CheckMarkedLiveFields(List<LibraryWorkspaceItem> chosen,
            List<WorkspaceBatchReviewRow> plan, DataGrid grid, Window dialog,
            TextBlock status, Button continueButton, Button checkButton,
            Button detailsButton, Button nativeApplyButton, Button nativeBatchButton,
            Dictionary<Guid,WorkspaceLiveBatchTarget> verifiedTargets,
            Dictionary<Guid,List<WorkspaceVerifiedFieldRow>> verifiedDetails)
        {
            if (checkingBatchFields)
            {
                if (batchFieldCancellation != null) batchFieldCancellation.Cancel();
                status.Text = "Canceling exact IGDB checks at the next safe boundary...";
                return;
            }
            if (scanning || checkingLiveFields || nativeApplying) return;
            CatalogCredentialData credentials;
            try { credentials = IdentityCredentials(); }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message, "IGDB exact-field inspection");
                return;
            }
            verifiedTargets.Clear();
            verifiedDetails.Clear();
            continueButton.IsEnabled = false;
            detailsButton.IsEnabled = false;
            detailsButton.Visibility = Visibility.Collapsed;
            nativeApplyButton.IsEnabled = false;
            nativeBatchButton.IsEnabled = false;
            var byId = plan.ToDictionary(x => x.GameId);
            var work = new List<Tuple<LibraryWorkspaceItem,WorkspaceLiveBatchTarget,WorkspaceBatchReviewRow>>();
            foreach (var item in chosen)
            {
                var row = byId[item.GameId];
                row.LiveCheck = "Not checked";
                row.AvailableFields = "—";
                try
                {
                    long catalogId;
                    if (!Int64.TryParse(item.CachedCatalogId, out catalogId) || catalogId <= 0 ||
                        String.IsNullOrWhiteSpace(item.CachedCatalogName))
                    {
                        row.LiveCheck = "No exact IGDB identity";
                        continue;
                    }
                    var input = CurrentIdentityInput(item);
                    ConfirmUnchangedLookup(input,item.CachedCatalogId,item.CachedCatalogName);
                    bool hadDraft = draftStates.ContainsKey(item.GameId);
                    var draft = CurrentDraft(item);
                    if (hadDraft && draft == null)
                        throw new InvalidOperationException("Staged draft is stale; recheck the game.");
                    var target = WorkspaceBatchLivePreflight.Capture(input,catalogId,
                        item.CachedCatalogName,draft == null ? null : draft.Draft);
                    row.LiveCheck = "Queued";
                    work.Add(Tuple.Create(item,target,row));
                }
                catch (Exception ex)
                {
                    row.LiveCheck = "Blocked: " + ex.Message;
                }
            }
            grid.Items.Refresh();
            if (work.Count == 0)
            {
                status.Text = "No saved, unchanged IGDB identities eligible for an exact-field check.";
                return;
            }
            var cancellation = new CancellationTokenSource();
            batchFieldCancellation = cancellation;
            checkingBatchFields = true;
            continueButton.IsEnabled = false;
            checkButton.Content = IconLabel("\uE8BB","Cancel field checks",Text);
            status.Text = "Checking 0 / " + work.Count + " exact IGDB records (read-only)...";
            Task.Run(() => {
                int checkedCount = 0, blocked = 0;
                string batchError = null;
                try
                {
                    var review = new WorkspaceIdentityReview(dataPath);
                    foreach (var entry in work)
                    {
                        if (cancellation.IsCancellationRequested) break;
                        CatalogGame exact = null;
                        string error = null;
                        try
                        {
                            exact = review.FetchExact(entry.Item2.IgdbId,
                                credentials.ClientId,credentials.ClientSecret,cancellation.Token);
                        }
                        catch (OperationCanceledException) { break; }
                        catch (Exception ex) { error = ex.Message; }
                        // Only the UI thread reads Playnite game/cache state.
                        var result = exact;
                        var failure = error;
                        dialog.Dispatcher.Invoke(new Action(() => {
                            if (!dialog.IsVisible) return;
                            try
                            {
                                if (failure != null) throw new InvalidOperationException(failure);
                                var current = CurrentIdentityInput(entry.Item1);
                                ConfirmUnchangedLookup(current,entry.Item2.IgdbId.ToString(
                                    System.Globalization.CultureInfo.InvariantCulture),entry.Item2.CatalogTitle);
                                WorkspaceBatchLivePreflight.EnsureCurrent(entry.Item2,current,
                                    entry.Item2.IgdbId,entry.Item2.CatalogTitle);
                                var compared = WorkspaceLiveComparison.Build(current,result,
                                    entry.Item2.IgdbId,entry.Item2.CatalogTitle);
                                if (entry.Item2.Draft != null)
                                    WorkspaceDraftPolicy.CheckComparison(current,compared);
                                var fields = compared.Fields.Where(f => f.MayAdd &&
                                    (entry.Item2.Draft == null ||
                                     WorkspaceDraftPolicy.Included(entry.Item2.Draft,f.Field)))
                                    .Select(f=>f.Field).ToList();
                                entry.Item3.AvailableFields = fields.Count == 0 ?
                                    "None available / selected" : String.Join(", ",fields);
                                var exactRows=new List<WorkspaceVerifiedFieldRow>();
                                exactRows.Add(new WorkspaceVerifiedFieldRow {
                                    GameId=entry.Item1.GameId,Game=entry.Item2.GameName,
                                    Platform=entry.Item2.Platform,IgdbId=entry.Item2.IgdbId.ToString(
                                        System.Globalization.CultureInfo.InvariantCulture),
                                    Field="Display title",Current=entry.Item2.GameName,
                                    Igdb=entry.Item2.CatalogTitle,
                                    PlannedAction=entry.Item3.TargetTitle
                                });
                                foreach (var field in compared.Fields.Where(x=>x.Field!="Display title"))
                                    exactRows.Add(new WorkspaceVerifiedFieldRow {
                                        GameId=entry.Item1.GameId,Game=entry.Item2.GameName,
                                        Platform=entry.Item2.Platform,IgdbId=entry.Item2.IgdbId.ToString(
                                            System.Globalization.CultureInfo.InvariantCulture),
                                        Field=field.Field,Current=field.Current,
                                        Igdb=field.PossibleAction,
                                        PlannedAction=WorkspaceDraftPolicy.ActionLabel(field,
                                            entry.Item2.Draft)
                                    });
                                verifiedDetails[entry.Item1.GameId]=exactRows;
                                entry.Item3.LiveCheck = "Fields checked · not approved";
                                verifiedTargets[entry.Item1.GameId] = entry.Item2;
                            }
                            catch (Exception ex)
                            {
                                verifiedTargets.Remove(entry.Item1.GameId);
                                verifiedDetails.Remove(entry.Item1.GameId);
                                entry.Item3.LiveCheck = "Blocked: " + ex.Message;
                                entry.Item3.AvailableFields = "—";
                                blocked++;
                            }
                            checkedCount++;
                            status.Text = "Checked " + checkedCount + " / " + work.Count +
                                " · " + blocked + " blocked. Read-only; final import approval still required.";
                            grid.Items.Refresh();
                        }));
                        if (cancellation.Token.WaitHandle.WaitOne(350)) break;
                    }
                }
                catch (Exception ex) { batchError = ex.Message; }
                var lastError = batchError;
                dialog.Dispatcher.BeginInvoke(new Action(() => {
                    checkingBatchFields = false;
                    batchFieldCancellation = null;
                    if (!dialog.IsVisible) return;
                    continueButton.IsEnabled = WorkspaceBatchLivePreflight.DetailedPlanReady(
                        plan, verifiedTargets,verifiedDetails) && !cancellation.IsCancellationRequested;
                    detailsButton.IsEnabled = continueButton.IsEnabled;
                    detailsButton.Visibility = detailsButton.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
                    if (nativeApplyButton != null)
                        nativeApplyButton.IsEnabled = continueButton.IsEnabled && plan.Count==1 &&
                            chosen.Count==1 && chosen[0].IdentityConfirmed && !nativeApplying;
                    if (nativeBatchButton != null)
                        nativeBatchButton.IsEnabled = continueButton.IsEnabled && plan.Count>1 &&
                            plan.Count==chosen.Count &&
                            chosen.All(x=>NativeBatchReady(x)) &&
                            !nativeApplying;
                    checkButton.Content = IconLabel("\uE721","Recheck exact fields (read-only)",Text);
                    status.Text = lastError != null ? "Exact-field check stopped: " + lastError :
                        cancellation.IsCancellationRequested ?
                        "Canceled after " + checkedCount + " / " + work.Count +
                        " records; no metadata changed." :
                        "Checked " + checkedCount + " / " + work.Count +
                        " records; " + blocked + " blocked. " +
                        (!continueButton.IsEnabled ?
                            "Resolve incomplete or blocked checks. No import approved or applied." :
                            (nativeApplyButton.IsEnabled || nativeBatchButton.IsEnabled) ?
                            "Fields checked. Review the exact plan before final Design C approval. " +
                            "Next: View field-by-field plan, then Apply for final approval." :
                            "Fields checked. Native import needs confirmed identity; Classic remains a fallback.");
                }));
            });
        }
        private static string ChangedFields(EnrichmentEntry item)
        {
            var parts = new List<string>();
            if (item.ChangedName) parts.Add("Title");
            if (item.ChangedDescription) parts.Add("Description");
            if (item.ChangedCover) parts.Add("Cover");
            if (item.ChangedIcon) parts.Add("Icon");
            if (item.ChangedBackground) parts.Add("Background");
            if (item.ChangedRelease) parts.Add("Release date");
            if (item.ChangedGenres) parts.Add("Genres");
            if (item.ChangedDevelopers) parts.Add("Developers");
            if (item.ChangedPublishers) parts.Add("Publishers");
            if (item.ChangedSeries) parts.Add("Series");
            return parts.Count == 0 ? "Other changes" : String.Join(", ", parts);
        }
        private void ShowWorkspaceRecoveryStatus()
        {
            try
            {
                var status = WorkspaceHistoryUndo.InspectRecovery(dataPath);
                var message = "Metadata journal operations: " + status.JournalOperations +
                    "\nUnfinished metadata operations: " + status.PendingOperations +
                    "\nArtwork stages: " + status.MediaStages +
                    "\nUnfinished artwork stages: " + status.OpenStages +
                    "\n\n" + (status.AllowsWrites ?
                        "Both recovery ledgers are consistent. This does NOT confirm that " +
                        "any specific Playnite field is unchanged." :
                        "Recovery required; imports and native undo are blocked:\n" +
                        String.Join("\n", status.Problems));
                api.Dialogs.ShowMessage(message, "Recovery status — read-only");
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Recovery ledgers could not be verified: " +
                    ex.Message + "\nNo records changed.", "Recovery status unavailable");
            }
        }

        private void UndoLatestFromHistory(Window historyDialog)
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying)
            {
                api.Dialogs.ShowMessage("Finish the active scan, field verification, or import before undo.",
                    "Undo unavailable"); return;
            }
            WorkspaceLatestUndoPreview latest;
            try { latest = WorkspaceHistoryUndo.PreviewLatest(dataPath); }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage(ex.Message + "\nNo metadata changed.",
                    "Undo blocked"); return;
            }
            if (latest == null)
            {
                api.Dialogs.ShowMessage("No completed modern import remains to undo. " +
                    "For legacy v0.4.0 operations use Classic Matcher.", "Nothing to undo"); return;
            }
            var confirm = api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton=false});
            confirm.Title = "Confirm undo of latest imported metadata";
            confirm.Width=680; confirm.Height=340;
            confirm.MinWidth=520; confirm.MinHeight=260;
            confirm.Owner=historyDialog;
            confirm.WindowStartupLocation=WindowStartupLocation.CenterOwner;
            confirm.Background=Background;
            var body=new StackPanel {Margin=new Thickness(18)};
            body.Children.Add(Label("Restore only the most recent imported game?", 18, Text));
            body.Children.Add(Label("Game: " + latest.AfterName +
                "\nPrevious Playnite title: " + latest.BeforeName +
                "\n\nThis is NOT a batch undo. The shared writer checks every field for " +
                "subsequent edits and refuses to overwrite those changes. Undo may revert " +
                "imported metadata and artwork, never ROM files or executables.", 12, Muted));
            var backup=new CheckBox {Content="I verified a restorable backup of my Playnite library AND extension data.",
                IsChecked=false,Foreground=Text,Margin=new Thickness(0,14,0,12)};
            body.Children.Add(backup);
            var actions=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right};
            var undo=ActionButton("Undo latest import", "\uE7A7", ()=>confirm.DialogResult=true,true);
            undo.IsEnabled=false;
            backup.Checked+=(s,e)=>undo.IsEnabled=true;
            backup.Unchecked+=(s,e)=>undo.IsEnabled=false;
            actions.Children.Add(ActionButton("Cancel — no changes", "\uE8BB", ()=>confirm.DialogResult=false));
            actions.Children.Add(undo);
            body.Children.Add(actions);
            confirm.Content=body;
            if (confirm.ShowDialog()!=true) return;
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying)
            {
                api.Dialogs.ShowMessage("A new operation began. Undo was not started.","Undo unavailable");return;
            }
            nativeApplying=true;
            try
            {
                var result=WorkspaceHistoryUndo.UndoLatest(api,dataPath,latest.OperationId);
                historyDialog.Close();
                SafeReload();
                api.Dialogs.ShowMessage(result,"Undo completed");
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Undo stopped: " + ex.Message +
                    "\nInspect History and Recovery before retrying.","Undo failed safely");
            }
            finally {nativeApplying=false;}
        }

        // A selected history range is permission only for a contiguous newest-first
        // prefix. Display order, selection order and cached row status grant no authority.
        private void UndoSelectedFromHistory(Window historyDialog, DataGrid historyGrid)
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying)
            {
                api.Dialogs.ShowMessage("Finish the active scan, field verification, or import before undo.",
                    "Undo unavailable"); return;
            }
            var chosen = historyGrid.SelectedItems.OfType<WorkspaceHistoryPreviewRow>().ToList();
            if (chosen.Count == 0)
            {
                api.Dialogs.ShowMessage("Select the most recent applied operation, then optionally " +
                    "the next older applied operations with Ctrl-click or Shift-click.",
                    "Select operations to undo"); return;
            }
            if (chosen.Any(x => x.OperationId == Guid.Empty || x.Status != "Recorded applied"))
            {
                api.Dialogs.ShowMessage("Only currently applied operations can be selected. " +
                    "Previously undone and interrupted records cannot be restored again.",
                    "Undo selection blocked"); return;
            }
            List<WorkspaceLatestUndoPreview> approved;
            try
            {
                approved = WorkspaceHistoryUndo.PreviewSelected(dataPath,
                    chosen.Select(x => x.OperationId));
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage(ex.Message + "\nNo metadata changed.",
                    "Undo selection blocked"); return;
            }
            // One itemized approval covers exactly the journal operations shown.
            var confirm = api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton=false});
            confirm.Title = "Confirm undo of " + approved.Count + " recent metadata imports";
            confirm.Width = 740; confirm.Height = 560;
            confirm.MinWidth = 550; confirm.MinHeight = 350;
            confirm.Owner = historyDialog;
            confirm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            confirm.Background = Background;
            var body = new DockPanel {Margin = new Thickness(18)};
            var heading = Label("Restore " + approved.Count + " most recent import(s)?", 18, Text);
            heading.FontWeight = FontWeights.SemiBold;
            heading.Margin = new Thickness(0,0,0,10);
            DockPanel.SetDock(heading, Dock.Top); body.Children.Add(heading);
            var explanation = Label("Newest first. A gap or changed journal blocks the entire request. " +
                "Each operation checks the live Playnite fields before undo; a conflict stops " +
                "the remaining operations. ROM files and executables are never changed.", 12, Muted);
            explanation.Margin = new Thickness(0,0,0,10);
            DockPanel.SetDock(explanation,Dock.Top); body.Children.Add(explanation);
            var footer = new StackPanel {Margin = new Thickness(0,12,0,0)};
            DockPanel.SetDock(footer,Dock.Bottom); body.Children.Add(footer);
            var backup = new CheckBox {
                Content = "I verified a restorable backup of my Playnite library AND extension data.",
                IsChecked = false, Foreground = Text, Margin = new Thickness(0,0,0,12)};
            footer.Children.Add(backup);
            var actions = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Right};
            var undoSelected = ActionButton("Undo " + approved.Count + " selected import(s)",
                "\uE7A7", () => confirm.DialogResult=true, true);
            undoSelected.IsEnabled = false;
            backup.Checked += (s,e) => undoSelected.IsEnabled=true;
            backup.Unchecked += (s,e) => undoSelected.IsEnabled=false;
            actions.Children.Add(ActionButton("Cancel — no changes", "\uE8BB",
                () => confirm.DialogResult=false));
            actions.Children.Add(undoSelected);
            footer.Children.Add(actions);
            var list = String.Join("\n\n", approved.Select((x,i) => (i+1) + ". " +
                x.AfterName + "\n   Previous: " + x.BeforeName + "\n   " + x.Fields));
            var details = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto};
            details.Content = Label(list, 12, Text);
            body.Children.Add(details);
            confirm.Content=body;
            if (confirm.ShowDialog()!=true || backup.IsChecked!=true) return;
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying)
            {
                api.Dialogs.ShowMessage("A new operation began. No undo was started.",
                    "Undo unavailable"); return;
            }
            // Freeze approved IDs before leaving the dialog. Revalidate the entire
            // prefix and both recovery ledgers after confirmation, on the UI thread.
            var ids = approved.Select(x => x.OperationId).ToList();
            nativeApplying=true;
            try
            {
                var result = WorkspaceHistoryUndo.UndoSelected(api,dataPath,ids);
                historyDialog.Close();
                SafeReload();
                api.Dialogs.ShowMessage(result, "Selected undo completed");
            }
            catch (Exception ex)
            {
                // Earlier operations may already have been undone. Always refresh
                // displayed state and report the exact committed prefix.
                historyDialog.Close();
                SafeReload();
                api.Dialogs.ShowErrorMessage("Selected undo stopped: " + ex.Message +
                    "\nInspect History and Recovery before retrying.", "Selected undo stopped safely");
            }
            finally { nativeApplying=false; }
        }

        // Ignore/restore is a private discovery preference, not a Playnite metadata update.
        // CatalogIgnored remains the single durable store; all file operations live in the service.
        private void ToggleFocusedIgnore()
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying) return;
            var selected = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            if (selected == null || selected.GameId == Guid.Empty ||
                selected.Group == WorkspaceTaskGroup.Updated ||
                selected.Group == WorkspaceTaskGroup.NeedsRecovery) return;
            var ignored = selected.Group == WorkspaceTaskGroup.Ignored;
            Game live;
            if (!knownGames.TryGetValue(selected.GameId, out live) || live == null ||
                api.Database.Games[selected.GameId] == null ||
                !String.Equals(api.Database.Games[selected.GameId].Name, selected.Title, StringComparison.Ordinal))
            {
                api.Dialogs.ShowMessage("The Playnite game changed. Reload the library before changing its ignored state.",
                    "Game state changed");
                SafeReload();
                return;
            }
            var verb = ignored ? "Restore" : "Ignore";
            var explanation = ignored ?
                "The game can return to pending review. Saved IGDB results and confirmations remain unchanged." :
                "Exclude this game from scans and review until you restore it. Its saved results are retained.";
            if (api.Dialogs.ShowMessage(verb + " this game?\n\n" + selected.Title +
                "\n\n" + explanation + "\n\nNo Playnite metadata or ROM files will change.",
                verb + " game", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                // Compare the expected current state against the *durable* store after consent.
                // Stale or malformed store data fails closed and is never silently overwritten.
                WorkspaceIgnoreDecision.Set(dataPath, selected.GameId, ignored, !ignored);
                markedIds.Remove(selected.GameId);
                draftStates.Remove(selected.GameId);
                freshStrictEvidence.Remove(selected.GameId);
                liveFieldResult = null;
                SafeReload();
                var revised = items.FirstOrDefault(x => x.GameId == selected.GameId);
                if (revised != null && table != null && table.ItemsSource != null)
                {
                    var view = CollectionViewSource.GetDefaultView(table.ItemsSource);
                    if (view != null && !view.Contains(revised)) scope.SelectedIndex = 5; // All games
                    table.SelectedItem = revised;
                    table.ScrollIntoView(revised);
                }
                if (scanStatus != null) scanStatus.Text = ignored ?
                    "Game restored for review. Saved lookup retained; no metadata changed." :
                    "Game ignored. Restore it from its details or All games; no metadata changed.";
                UpdateInspector();
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Ignore/restore state could not be verified: " + ex.Message +
                    "\nReload the library and inspect the ignored status before retrying.",
                    "Ignored list unavailable");
                SafeReload();
            }
        }

        // Bulk triage affects only ignored-ID preferences. All selected IDs must
        // remain eligible and unchanged; mixed state is refused rather than skipped.
        private void SetHighlightedIgnored(bool ignore)
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying ||
                table == null) return;
            var selected = Highlighted();
            if (selected.Count == 0)
            {
                api.Dialogs.ShowMessage("Highlight one or more games with Ctrl/Shift-click.",
                    "Bulk ignore / restore");return;
            }
            if (selected.Any(x => x.GameId == Guid.Empty) ||
                selected.Select(x => x.GameId).Distinct().Count() != selected.Count ||
                selected.Any(x => ignore ?
                    x.Group == WorkspaceTaskGroup.Ignored ||
                    x.Group == WorkspaceTaskGroup.Updated ||
                    x.Group == WorkspaceTaskGroup.NeedsRecovery :
                    x.Group != WorkspaceTaskGroup.Ignored))
            {
                api.Dialogs.ShowMessage(ignore ?
                    "Select only unresolved, non-ignored games. Updated and recovery-needed games cannot be ignored." :
                    "Select only ignored games to restore. Mixed selections cannot be changed in bulk.",
                    "Bulk ignore / restore");return;
            }
            // No partial changes if a Playnite game was renamed, removed, or moved to
            // a different platform after the workspace loaded.
            foreach (var row in selected)
            {
                Game game;
                if (!knownGames.TryGetValue(row.GameId,out game) || game == null)
                {api.Dialogs.ShowMessage("Library changed; reload before changing ignored games.",
                    "Bulk triage blocked");return;}
                var live = api.Database.Games[row.GameId];
                if (live == null ||
                    !String.Equals(live.Name,row.Title,StringComparison.Ordinal) ||
                    !String.Equals(ScanInput(live).Platform,row.Platform,StringComparison.Ordinal))
                {api.Dialogs.ShowMessage("A selected game changed; reload before changing ignored games.",
                    "Bulk triage blocked");return;}
            }
            string verb=ignore ? "Ignore" : "Restore";
            string names=String.Join("\n",selected.Take(10).Select(x=>
                " • " + x.Title + " [" + x.Platform + "]"));
            if (selected.Count>10) names += "\n ...and " + (selected.Count-10) + " more highlighted games";
            if (api.Dialogs.ShowMessage(verb + " " + selected.Count + " highlighted game(s)?\n\n"+
                names + "\n\nOnly ignored-game preferences will change. " +
                "Saved matches and confirmed identities remain available. " +
                "No Playnite metadata, journal or ROM file changes.",
                verb + " highlighted games",MessageBoxButton.YesNo,
                MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            try
            {
                // Revalidate the complete selected set and its durable membership
                // after approval. A single changed state rejects the entire batch.
                if (selected.Any(row => {
                    var live=api.Database.Games[row.GameId];
                    return live==null ||
                        !String.Equals(live.Name,row.Title,StringComparison.Ordinal) ||
                        !String.Equals(ScanInput(live).Platform,row.Platform,StringComparison.Ordinal);
                })) throw new InvalidOperationException("Playnite changed since approval.");
                WorkspaceIgnoreDecision.SetMany(dataPath,
                    selected.Select(x=>x.GameId),!ignore,ignore);
                foreach(var row in selected)
                {
                    markedIds.Remove(row.GameId);
                    draftStates.Remove(row.GameId);
                    freshStrictEvidence.Remove(row.GameId);
                }
                liveFieldResult=null;
                SafeReload();
                if (scanStatus != null) scanStatus.Text=verb + "d " + selected.Count +
                    " game(s). Existing IGDB results preserved; no Playnite metadata changed.";
                UpdateSelectionSummary();
            }
            catch(Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Bulk ignored-state change was stopped: " + ex.Message +
                    "\nReload to inspect the durable list before retrying.","Bulk triage stopped");
                SafeReload();
            }
        }

        // Presentation-only history visibility uses Classic's existing atomic
        // preference store. Never delete, reorder or edit write-ahead journal entries.
        private void ChangeHistoryVisibility(DataGrid historyGrid, bool hide, Action refresh)
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying) return;
            var selected = historyGrid.SelectedItems.OfType<WorkspaceHistoryPreviewRow>()
                .Where(x=>x!=null && x.OperationId!=Guid.Empty).ToList();
            if (selected.Count==0 || selected.Any(x=>x.Hidden==hide))
            {
                api.Dialogs.ShowMessage(hide ?
                    "Select visible operations to hide from this view." :
                    "Select hidden operations (enable Include hidden) to restore.",
                    "Select history operations"); return;
            }
            var ids=selected.Select(x=>x.OperationId).Distinct().ToList();
            if (ids.Count != selected.Count)
            {api.Dialogs.ShowMessage("Duplicate history operations in selection. Reload history.",
                "History state changed");return;}
            if (hide && api.Dialogs.ShowMessage("Hide " + ids.Count +
                " operation(s) from the normal History view?\n\n" +
                "This does not delete journal entries or change metadata. " +
                "Hidden operations remain in the undo stack and can be restored here.",
                "Hide history entries",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return;
            try
            {
                // Shared service verifies fresh journal identity, stale preferences,
                // atomic persistence and durable re-read. UI never writes files.
                WorkspaceHistoryVisibility.Set(dataPath,ids,hide);
                refresh();
            }
            catch(Exception ex)
            {
                api.Dialogs.ShowErrorMessage("History visibility could not be updated: "+ex.Message+
                    "\nNo import or undo was initiated.","History visibility");
            }
        }

        private void ShowHistoryPreview()
        {
            try
            {
                var journal = new EnrichmentJournal(dataPath);
                // The same visibility preferences used by Classic: hidden operations
                // remain in the immutable undo history and newest-first stack.
                var hiddenStore = new HistoryVisibilityStore(dataPath);
                var hiddenIds = hiddenStore.Load();
                var history = journal.AllEntries().Select(x => new WorkspaceHistoryPreviewRow {
                    Game = !String.IsNullOrWhiteSpace(x.AfterName) ? x.AfterName : x.BeforeName,
                    OperationId = x.OperationId,
                    Hidden = hiddenIds.Contains(x.OperationId),
                    Status = x.Undone ? "Undone" : x.Committed ? "Recorded applied" : "Pending / interrupted",
                    Fields = ChangedFields(x),
                    When = x.CommittedUtc.Year > 2000 ? x.CommittedUtc.ToLocalTime().ToString("g") : "Unknown"
                }).ToList();
                var dialog = api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton = false});
                dialog.Title = "Update history & safe undo";
                dialog.Width = 1020; dialog.Height = 660;
                dialog.MinWidth = 680; dialog.MinHeight = 420;
                dialog.Owner = window; dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.Background = Background;
                var root = new DockPanel {Margin = new Thickness(15)};
                var title = Label(history.Count + " recorded operations · journal history", 19, Text);
                title.FontWeight = FontWeights.SemiBold; title.Margin = new Thickness(0, 0, 0, 8);
                DockPanel.SetDock(title, Dock.Top); root.Children.Add(title);
                var help = Label("Review recorded changes below. Undo latest or select contiguous newest-first " +
                    "operations for conditional undo. Every field and recovery ledger is rechecked " +
                    "before approval; no changes occur by opening History.", 12, Muted);
                help.Margin = new Thickness(0, 0, 0, 12);
                DockPanel.SetDock(help, Dock.Top); root.Children.Add(help);
                var bottom = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 12, 0, 0)};
                DockPanel.SetDock(bottom, Dock.Bottom); root.Children.Add(bottom);
                bottom.Children.Add(ActionButton("Recovery status", "\uE721", ShowWorkspaceRecoveryStatus));
                bottom.Children.Add(ActionButton("Undo latest import...", "\uE7A7", () => UndoLatestFromHistory(dialog), true));
                bottom.Children.Add(ActionButton("Close history", "\uE8BB", () => dialog.Close()));
                // Search/status/hide are presentation only. Filtering never changes
                // journal order or changes which operation is legally undoable.
                var filters = new WrapPanel {Margin = new Thickness(0,0,0,10)};
                DockPanel.SetDock(filters,Dock.Top); root.Children.Add(filters);
                filters.Children.Add(Label("Show",12,Muted));
                var historyState = new ComboBox {Width=112,Height=34,Margin=new Thickness(8,0,12,0)};
                foreach(var value in new [] {HistoryViewFilter.All,HistoryViewFilter.Applied,HistoryViewFilter.Undone})
                    historyState.Items.Add(value);
                historyState.SelectedIndex=0; filters.Children.Add(historyState);
                var historySearch = new TextBox {Width=200,Height=34,
                    Margin=new Thickness(0,0,12,0),ToolTip="Search game names, dates and changed fields"};
                filters.Children.Add(historySearch);
                var includeHidden = new CheckBox {Content="Include hidden",Foreground=Text,
                    VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,12,0)};
                filters.Children.Add(includeHidden);
                var grid = new DataGrid {AutoGenerateColumns = false, IsReadOnly = true,
                    SelectionMode = DataGridSelectionMode.Extended, HeadersVisibility = DataGridHeadersVisibility.Column,
                    Background = Panel, Foreground = Text, RowBackground = Panel,
                    AlternatingRowBackground = AlternateRow, AlternationCount = 2,
                    ColumnHeaderHeight = 34, RowHeight = 31, GridLinesVisibility = DataGridGridLinesVisibility.None};
                AddColumn(grid, "Recorded", "When", 140);
                AddColumn(grid, "Game", "Game", 240);
                AddColumn(grid, "State", "Status", 160);
                AddColumn(grid, "Fields", "Fields", 360);
                AddColumn(grid, "Visibility", "VisibilityLabel", 100);
                // Rebuild only the view. The journal remains complete, including
                // hidden or filtered operations used by the undo safety policy.
                Action refreshHistoryView = () => {
                    var visibility = hiddenStore.Load();
                    foreach(var row in history) row.Hidden=visibility.Contains(row.OperationId);
                    string state = historyState.SelectedItem as string ?? HistoryViewFilter.All;
                    var visible = history.Where(x => (includeHidden.IsChecked==true || !x.Hidden) &&
                        HistoryViewFilter.Includes(x.Status=="Recorded applied" ?
                            HistoryViewFilter.Applied : x.Status=="Undone" ?
                            HistoryViewFilter.Undone : "Pending",x.Game,x.Fields,x.When,
                            state,historySearch.Text)).ToList();
                    grid.ItemsSource = visible;
                    title.Text=visible.Count+" of "+history.Count+" recorded operations · journal history";
                };
                historyState.SelectionChanged += (sender,args) => refreshHistoryView();
                historySearch.TextChanged += (sender,args) => refreshHistoryView();
                includeHidden.Checked += (sender,args) => refreshHistoryView();
                includeHidden.Unchecked += (sender,args) => refreshHistoryView();
                Button visibilityButton = null;
                visibilityButton = ActionButton("Visibility", "\uE8D4", () => {
                    var menu = new ContextMenu {PlacementTarget = visibilityButton,
                        Placement = PlacementMode.Bottom};
                    var hide = new MenuItem {Header = "Hide selected from this view..."};
                    hide.Click += (sender,args) => ChangeHistoryVisibility(grid,true,refreshHistoryView);
                    menu.Items.Add(hide);
                    var restore = new MenuItem {Header = "Restore selected hidden operations..."};
                    restore.Click += (sender,args) => ChangeHistoryVisibility(grid,false,refreshHistoryView);
                    menu.Items.Add(restore);
                    menu.IsOpen = true;
                });
                visibilityButton.ToolTip = "View-only hide/restore; journal records are never deleted.";
                filters.Children.Add(visibilityButton);
                bottom.Children.Insert(2, ActionButton("Undo selected recent...", "\uE7A7",
                    () => UndoSelectedFromHistory(dialog,grid)));
                refreshHistoryView();
                root.Children.Add(grid);
                dialog.Content = root; dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage("Could not read update history safely: " + ex.Message +
                    "\nNo records were changed.", "History could not be loaded");
            }
        }
        // This is guidance only. It never certifies the IGDB identity, infers that
        // a field exists remotely, or approves a Playnite write.
        private static string SavedEvidence(LibraryWorkspaceItem row)
        {
            if (row == null || !row.FromSaved) return "No saved IGDB lookup for this game.";
            string title = row.CachedCatalogName ?? "(none)";
            string identity = String.IsNullOrWhiteSpace(row.CachedCatalogId) ? "No IGDB ID" :
                "IGDB ID " + row.CachedCatalogId;
            string score = String.IsNullOrWhiteSpace(row.CachedScore) ? "" :
                " · ranking score " + row.CachedScore + " (not a probability)";
            DateTime checkedUtc;
            string age = DateTime.TryParse(row.CachedScannedUtc, out checkedUtc) ?
                " · lookup " + checkedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "";
            string evidence = row.CachedEvidence ?? "";
            if (evidence.Length > 380) evidence = evidence.Substring(0, 380) + "…";
            return title + " · " + identity + score + age + "\n" +
                "Saved candidate; identity not newly confirmed. " + evidence;
        }
        private static string NextStepFor(LibraryWorkspaceItem row)
        {
            if (row == null) return "Select a game to see its next step.";
            int missing = CatalogEnrichmentNeeds.Count(row.MissingMask);
            switch (row.Group)
            {
                case WorkspaceTaskGroup.Suggestions:
                    if (missing == 1)
                        return row.Missing + " is missing in Playnite. Refresh this IGDB suggestion " +
                            "here, then use the review flow to inspect actual available values.";
                    return missing + " metadata fields are missing in Playnite. Refresh this IGDB " +
                        "suggestion here, then review available values before any approval.";
                case WorkspaceTaskGroup.NeedsChoice:
                    return "The suggested identity needs your decision. Check the title, platform and edition. Search for the correct " +
                        "IGDB identity or confirm this saved candidate here. Confirmation is not import approval.";
                case WorkspaceTaskGroup.NotFound:
                    return "No catalog match was found. Search alternative IGDB titles or enter an exact " +
                        "IGDB ID here. The chosen record still needs explicit identity confirmation.";
                case WorkspaceTaskGroup.CouldNotCheck:
                    return "The previous lookup did not complete. Retry the IGDB lookup here " +
                        "when the service is available.";
                case WorkspaceTaskGroup.CheckNeeded:
                    return "This game has not been checked against IGDB. Use Scan pending " +
                        "to look for matching records and missing metadata.";
                case WorkspaceTaskGroup.Updated:
                    return "This game has a recorded update. Open History to inspect " +
                        "the applied fields or supported undo options.";
                case WorkspaceTaskGroup.Ignored:
                    return "This game is excluded from Smart Metadata Matcher review. " +
                        "Use Restore this game below to include it again. Your saved match is retained.";
                case WorkspaceTaskGroup.NeedsRecovery:
                    return "A recovery record needs attention. Open Advanced > Inspect recovery " +
                        "ledgers (read-only) before making changes. Classic retains legacy recovery tools.";
                default:
                    return "No missing supported fields were found in Playnite.";
            }
        }
        private bool LiveFieldPreviewMatches(LibraryWorkspaceItem row)
        {
            return row != null && liveFieldResult != null && row.FromSaved &&
                liveFieldResult.GameId == row.GameId &&
                liveFieldResult.MissingMask == row.MissingMask &&
                String.Equals(liveFieldResult.GameName, row.Title, StringComparison.Ordinal) &&
                String.Equals(liveFieldResult.Platform, row.Platform, StringComparison.Ordinal) &&
                String.Equals(liveFieldResult.IgdbId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    row.CachedCatalogId, StringComparison.Ordinal) &&
                String.Equals(liveFieldResult.IgdbName, row.CachedCatalogName, StringComparison.Ordinal);
        }
        private WorkspaceDraftState CurrentDraft(LibraryWorkspaceItem row)
        {
            if (row == null) return null;
            WorkspaceDraftState saved;
            if (!draftStates.TryGetValue(row.GameId, out saved)) return null;
            Game known;
            long id;
            if (!knownGames.TryGetValue(row.GameId, out known) || known == null ||
                !row.FromSaved || !row.CanMark ||
                !Int64.TryParse(row.CachedCatalogId, out id) || id <= 0 ||
                !saved.Matches(ScanInput(known), id, row.CachedCatalogName))
            {
                draftStates.Remove(row.GameId);
                return null;
            }
            return saved;
        }

        // Edit only a previously checked exact ID. This dialog stages no metadata
        // writes, identity decisions, durable drafts or batch approval.
        private void EditFocusedDraft()
        {
            if (scanning || checkingLiveFields || table == null) return;
            var selected = table.SelectedItem as LibraryWorkspaceItem;
            if (!LiveFieldPreviewMatches(selected)) return;
            CatalogInput before;
            WorkspaceLiveComparisonResult compared = liveFieldResult;
            try
            {
                before = CurrentIdentityInput(selected);
                ConfirmUnchangedLookup(before, selected.CachedCatalogId, selected.CachedCatalogName);
                WorkspaceDraftPolicy.CheckComparison(before, compared);
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message, "Edit proposed metadata");
                return;
            }
            var existing = CurrentDraft(selected);
            var draft = existing == null ? WorkspaceDraftPolicy.New(before, compared) :
                existing.Draft.Copy();
            var dialog = new Window {Title = "Choose fields and custom text — " + before.Name,
                Width = 760, Height = 750, MinWidth = 565, MinHeight = 480,
                Owner = window, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Background, ResizeMode = ResizeMode.CanResize};
            var root = new DockPanel {Margin = new Thickness(16)};
            var heading = new StackPanel {Margin = new Thickness(0, 0, 0, 12)};
            DockPanel.SetDock(heading, Dock.Top);root.Children.Add(heading);
            var title = Label("Choose fields and optional custom text", 19, Text);
            title.FontWeight = FontWeights.SemiBold;heading.Children.Add(title);
            heading.Children.Add(Label("Playnite: " + before.Name + " · " + before.Platform, 12, Muted));
            heading.Children.Add(Label("IGDB: " + compared.IgdbName + " · ID " + compared.IgdbId, 12, Accent));
            var warning = Label("Check a field to include its IGDB value; those values are not inline-editable. " +
                "Custom title and description are editable below. This session-only draft does NOT update " +
                "Playnite or confirm identity; importing still requires fresh checks, a backup and final approval.",
                12, Warning);
            warning.Margin = new Thickness(0, 8, 0, 0);
            heading.Children.Add(warning);
            var footer = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)};
            DockPanel.SetDock(footer, Dock.Bottom);root.Children.Add(footer);
            var scroll = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled};
            root.Children.Add(scroll);
            var body = new StackPanel {Margin = new Thickness(1, 0, 8, 2)};
            scroll.Content = body;
            var choices = new Dictionary<string, CheckBox>(StringComparer.Ordinal);
            body.Children.Add(Label("CUSTOM TEXT · OPTIONAL", 12, Accent));
            var customTitle = new CheckBox {Content = "Propose a different Playnite display title",
                IsChecked = draft.CustomTitleEnabled, Foreground = Text, FontSize = 13,
                Margin = new Thickness(0, 12, 0, 4)};
            body.Children.Add(customTitle);
            var titleInput = new TextBox {Text = draft.CustomTitleEnabled ? draft.CustomTitle ?? "" : before.Name,
                Height = ToolbarHeight, IsEnabled = draft.CustomTitleEnabled,
                Padding = new Thickness(8, 6, 8, 4), Background = PanelRaised,
                Foreground = Text, BorderBrush = Stroke, FontSize = 13};
            customTitle.Checked += (sender, args) => titleInput.IsEnabled = true;
            customTitle.Unchecked += (sender, args) => titleInput.IsEnabled = false;
            body.Children.Add(titleInput);
            body.Children.Add(Label("A custom title is collision-checked and changes only Playnite's " +
                "display name when separately approved. ROM files and launch paths stay unchanged.", 11, Muted));
            var customDescription = new CheckBox {Content = "Supply a custom description",
                IsChecked = draft.CustomDescriptionEnabled, Foreground = Text, FontSize = 13,
                Margin = new Thickness(0, 13, 0, 4)};
            body.Children.Add(customDescription);
            var descriptionInput = new TextBox {Text = draft.CustomDescription ?? "",
                Height = 105, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsEnabled = draft.CustomDescriptionEnabled, MaxLength = 12000,
                Padding = new Thickness(8, 5, 8, 5), Background = PanelRaised,
                Foreground = Text, BorderBrush = Stroke, FontSize = 13};
            customDescription.Checked += (sender, args) => descriptionInput.IsEnabled = true;
            customDescription.Unchecked += (sender, args) => descriptionInput.IsEnabled = false;
            body.Children.Add(descriptionInput);
            body.Children.Add(Label("Custom descriptions can replace an existing Playnite " +
                "description only after a separate explicit import approval.", 11, Warning));
            var fieldHeading = Label("IGDB VALUES · CHECK TO INCLUDE", 12, Muted);
            fieldHeading.FontWeight = FontWeights.SemiBold;
            fieldHeading.Margin = new Thickness(0, 18, 0, 7);
            body.Children.Add(fieldHeading);
            foreach (var field in compared.Fields.Where(x => x.Field != "Display title"))
            {
                var panel = Frame(Panel, SoftStroke, new Thickness(8));
                panel.Margin = new Thickness(0, 0, 0, 6);
                var block = new StackPanel();
                panel.Child = block;
                var box = new CheckBox {Content = field.Field,
                    IsChecked = field.MayAdd && WorkspaceDraftPolicy.Included(draft, field.Field),
                    IsEnabled = field.MayAdd, Foreground = Text, FontSize = 13,
                    FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center,
                    MinHeight = 26};
                block.Children.Add(box);
                var detail = Label(field.Current + " · " + field.PossibleAction, 11,
                    field.MayAdd ? Muted : Warning);
                detail.Margin = new Thickness(23, 0, 0, 2);
                block.Children.Add(detail);
                choices.Add(field.Field, box);
                body.Children.Add(panel);
            }
            var error = Label("", 12, Warning);
            error.Margin = new Thickness(0, 10, 0, 0);
            body.Children.Add(error);
            var save = ActionButton("Save draft", "\uE74E", () =>
            {
                foreach (var option in choices)
                    if (option.Value.IsEnabled)
                        WorkspaceDraftPolicy.SetIncluded(draft, option.Key,
                            option.Value.IsChecked == true);
                draft.CustomTitleEnabled = customTitle.IsChecked == true;
                draft.CustomTitle = draft.CustomTitleEnabled ? titleInput.Text : null;
                draft.CustomDescriptionEnabled = customDescription.IsChecked == true;
                draft.CustomDescription = draft.CustomDescriptionEnabled ? descriptionInput.Text : null;
                try
                {
                    var current = CurrentIdentityInput(selected);
                    ConfirmUnchangedLookup(current, selected.CachedCatalogId, selected.CachedCatalogName);
                    if (current.Id != before.Id || current.MissingMask != before.MissingMask ||
                        current.LocalReleaseYear != before.LocalReleaseYear)
                        throw new InvalidOperationException("Playnite fields changed while the draft was open.");
                    var library = api.Database.Games.Where(g => g != null).Select(g =>
                        Tuple.Create(g.Id, g.Name, g.Platforms == null ? "" :
                            String.Join(", ", g.Platforms.Select(p => p.Name)))).ToList();
                    var saved = WorkspaceDraftPolicy.Save(current, compared, draft, library);
                    if (!LiveFieldPreviewMatches(selected))
                        throw new InvalidOperationException("IGDB evidence changed. Recheck fields first.");
                    draftStates[selected.GameId] = saved;
                    dialog.DialogResult = true;
                }
                catch (Exception ex) {error.Text = ex.Message;}
            }, true);
            footer.Children.Add(save);
            footer.Children.Add(ActionButton("Reset draft", "\uE711", () =>
            {
                draftStates.Remove(selected.GameId);
                dialog.DialogResult = true;
            }));
            footer.Children.Add(ActionButton("Cancel", "\uE8BB", () => dialog.Close()));
            dialog.Content = root;
            if (dialog.ShowDialog() == true)
            {
                UpdateInspector();
                scanStatus.Text = "Draft preferences updated for " + before.Name +
                    ". Session-only proposal; no import or metadata write performed.";
            }
        }

        private void UpdateInspector()
        {
            if (detailTitle == null) return;
            var row = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            detailTitle.Text = row == null ? "Choose a game" : row.Title;
            detailPlatform.Text = row == null ? "" : row.Platform +
                (String.IsNullOrWhiteSpace(row.Source) ? "" : "  ·  Source: " + row.Source);
            detailStatus.Text = row == null ? "Select a row to see what needs attention." : row.Status;
            detailStatus.Foreground = row == null ? Muted : GroupColor(row.Group);
            detailMatch.Text = row == null ? "—" : row.Candidate;
            detailMissing.Text = row == null ? "—" : row.Missing;
            detailHelp.Text = NextStepFor(row);
            if (detailEvidence != null) detailEvidence.Text = SavedEvidence(row);
            if (refreshFocusedButton != null) refreshFocusedButton.IsEnabled = !scanning && !checkingLiveFields && row != null &&
                row.FromSaved && row.CanMark;
            if (identitySearchButton != null)
                identitySearchButton.IsEnabled = !scanning && !checkingLiveFields && row != null && row.FromSaved && row.CanMark;
            long savedIgdbId;
            bool hasSavedId = row != null && row.FromSaved && row.CanMark &&
                Int64.TryParse(row.CachedCatalogId, out savedIgdbId) && savedIgdbId > 0 &&
                !String.IsNullOrWhiteSpace(row.CachedCatalogName);
            if (identityConfirmButton != null)
                identityConfirmButton.IsEnabled = !scanning && !checkingLiveFields && hasSavedId && !row.IdentityConfirmed;
            if (ignoreRestoreButton != null)
            {
                bool canIgnore = row != null && row.Group != WorkspaceTaskGroup.Updated &&
                    row.Group != WorkspaceTaskGroup.NeedsRecovery;
                ignoreRestoreButton.Visibility = canIgnore ? Visibility.Visible : Visibility.Collapsed;
                ignoreRestoreButton.IsEnabled = canIgnore && !scanning && !checkingLiveFields &&
                    !checkingBatchFields && !nativeApplying;
                ignoreRestoreButton.Content = IconLabel("\uE711",
                    row != null && row.Group == WorkspaceTaskGroup.Ignored ?
                    "Restore this game" : "Ignore this game", Text);
            }
            if (identityForgetButton != null)
            {
                identityForgetButton.Visibility = row != null && row.IdentityConfirmed ?
                    Visibility.Visible : Visibility.Collapsed;
                identityForgetButton.IsEnabled = !scanning && !checkingLiveFields && row != null && row.IdentityConfirmed;
            }
            if (detailVerification != null)
            {
                detailVerification.Text = row != null && row.IdentityConfirmed ?
                    "Identity confirmed by you · live import preflight still required" :
                    row != null && row.FromSaved && row.CanMark ?
                    "Live check required before applying metadata" : "";
                detailVerification.Visibility = detailVerification.Text.Length == 0 ?
                    Visibility.Collapsed : Visibility.Visible;
            }
            bool hasLiveFields = LiveFieldPreviewMatches(row);
            var draftState = CurrentDraft(row);
            if (reviewFocusedButton != null)
            {
                bool needsMatch = NeedsManualIdentity(row);
                reviewFocusedButton.IsEnabled = !BusyForNavigation() && (hasSavedId || needsMatch);
                reviewFocusedButton.Content = IconLabel("\uE8A7", needsMatch ?
                    "Find IGDB match..." : "Review this game...", Background);
                reviewFocusedButton.ToolTip = needsMatch ?
                    "Choose an exact IGDB identity first. This saves only a suggestion; review and import approval are separate." :
                    "Review only the focused game; import still requires live checks and separate approval.";
            }
            if (editDraftButton != null)
            {
                editDraftButton.IsEnabled = !scanning && !checkingLiveFields && hasLiveFields;
                editDraftButton.Content = IconLabel("\uE70F", !hasLiveFields ?
                    "Check IGDB fields first" : draftState == null ?
                    "Choose fields / custom text" : "Edit saved field choices", Text);
            }
            if (draftStatus != null)
            {
                draftStatus.Text = draftState == null ?
                    "No draft saved. Check live fields to choose imports or type a custom title/description." :
                    "Session draft · " + (hasLiveFields ? WorkspaceDraftPolicy.IncludedAvailable(
                        liveFieldResult, draftState.Draft).ToString() + " possible additions included" :
                        "recheck IGDB fields to edit") +
                    (draftState.Draft.CustomTitleEnabled ? " · proposed title" : "") +
                    (draftState.Draft.CustomDescriptionEnabled ? " · custom description" : "") +
                    ". Not import approval.";
                draftStatus.Foreground = draftState == null ? Muted : Accent;
            }
            if (detailFieldSummary != null)
            {
                int count = row == null ? 0 : CatalogEnrichmentNeeds.Count(row.MissingMask);
                detailFieldSummary.Text = row == null ? "No game selected" :
                    count + (count == 1 ? " missing field" : " missing fields") +
                    (hasLiveFields ? " · " + liveFieldResult.PossibleAdditions +
                        " possible addition" + (liveFieldResult.PossibleAdditions == 1 ? "" : "s") : "");
            }
            if (checkLiveFieldsButton != null)
            {
                checkLiveFieldsButton.IsEnabled = checkingLiveFields ||
                    (!scanning && row != null && row.FromSaved && row.CanMark &&
                    !String.IsNullOrWhiteSpace(row.CachedCatalogId));
                checkLiveFieldsButton.Content = IconLabel("\uE721", checkingLiveFields ?
                    "Cancel field check" : hasLiveFields ? "Recheck IGDB fields" :
                    "Check IGDB field availability", Text);
            }
            if (liveFieldStatus != null)
            {
                liveFieldStatus.Text = checkingLiveFields ? "Checking exact IGDB record (read-only)..." :
                    hasLiveFields ? "Exact IGDB record fetched this session. Potential additions only: " +
                    "media downloads, identity and import still require verification and approval." :
                    "Read-only field preview. Check IGDB fields above to enable draft editing.";
                liveFieldStatus.Foreground = hasLiveFields ? Good : Muted;
            }
            if (fieldComparison != null)
                fieldComparison.ItemsSource = hasLiveFields ? liveFieldResult.Fields.Select(x =>
                    new WorkspaceFieldPreviewRow {Field = x.Field, Current = x.Current,
                        Proposed = WorkspaceDraftPolicy.ActionLabel(x,
                            draftState == null ? null : draftState.Draft)}).ToList() : FieldPreview(row);
            if (previousGameButton != null)
            {
                var visible = table.Items.Cast<object>().OfType<LibraryWorkspaceItem>().ToList();
                int position = visible.FindIndex(x => row != null && x.GameId == row.GameId);
                previousGameButton.IsEnabled = position > 0;
                nextGameButton.IsEnabled = position >= 0 && position < visible.Count - 1;
            }
            if (existingCover == null) return;
            existingCover.Source = null; existingIcon.Source = null; existingBackground.Source = null;
            headlineCover.Source = null;
            headlineArtFallback.Visibility = Visibility.Visible;
            ClearSuggestedArtwork();
            if (row == null)
            {
                artworkNote.Text = "Choose a game to see artwork already in Playnite.";
                suggestedArtButton.IsEnabled = false;
                return;
            }
            Game game;
            if (knownGames.TryGetValue(row.GameId, out game))
            {
                bool compact = ShowExisting(headlineCover, game.CoverImage, 180);
                headlineArtFallback.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
                // Decode only the small cover during list navigation. The full
                // gallery's extra images are loaded when the user expands it.
                bool galleryOpen = artworkExpander != null && artworkExpander.IsExpanded;
                existingCover.Source = galleryOpen ? headlineCover.Source : null;
                bool icon = galleryOpen && ShowExisting(existingIcon, game.Icon, 110);
                bool background = galleryOpen && ShowExisting(existingBackground, game.BackgroundImage, 230);
                artworkNote.Text = !galleryOpen ? "Expand to see current artwork. Online previews are opt-in." :
                    compact || icon || background ?
                    "Existing Playnite artwork is above. Missing images are blank." :
                    "No local artwork to display. Internet artwork is not loaded automatically.";
            }
            suggestedArtButton.IsEnabled = row.FromSaved &&
                (ValidImageId(row.SuggestedCoverId) || ValidImageId(row.SuggestedScreenshotId));
        }

        // Verify the live Playnite state BEFORE changing the lookup or confirmation
        // store. This does not certify a game identity or an import destination.
        private CatalogInput CurrentIdentityInput(LibraryWorkspaceItem selected)
        {
            if (selected == null || !selected.FromSaved || !selected.CanMark)
                throw new InvalidOperationException("Select a saved, unresolved game first.");
            Game original;
            if (!knownGames.TryGetValue(selected.GameId, out original) || original == null)
                throw new InvalidOperationException("Reload the library before reviewing this game.");
            var latest = api.Database.Games[selected.GameId];
            if (latest == null) throw new InvalidOperationException("Game removed from Playnite. Reload first.");
            var before = ScanInput(original);
            var live = ScanInput(latest);
            if (!String.Equals(live.Name, selected.Title, StringComparison.Ordinal) ||
                !String.Equals(live.Name, before.Name, StringComparison.Ordinal) ||
                !String.Equals(live.Platform, before.Platform, StringComparison.Ordinal) ||
                live.MissingMask != before.MissingMask || live.MissingCount <= 0 ||
                live.LocalReleaseYear != before.LocalReleaseYear)
                throw new InvalidOperationException("The game or supported metadata changed. Reload (F5) first.");
            return live;
        }
        // An explicit live field check never approves an IGDB identity or an import.
        // Validate saved lookup and current Playnite fields before AND after HTTPS.
        private void CheckLiveFieldAvailability()
        {
            if (checkingLiveFields)
            {
                if (liveFieldCancellation != null) liveFieldCancellation.Cancel();
                return;
            }
            if (scanning || table == null) return;
            var selected = table.SelectedItem as LibraryWorkspaceItem;
            CatalogInput input;
            CatalogCredentialData credentials;
            long igdbId;
            try
            {
                input = CurrentIdentityInput(selected);
                if (!Int64.TryParse(selected.CachedCatalogId, out igdbId) || igdbId <= 0 ||
                    String.IsNullOrWhiteSpace(selected.CachedCatalogName))
                    throw new InvalidOperationException("Choose a saved IGDB identity first.");
                ConfirmUnchangedLookup(input, selected.CachedCatalogId, selected.CachedCatalogName);
                credentials = IdentityCredentials();
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowMessage(ex.Message, "IGDB field comparison");
                return;
            }
            var expectedId = selected.CachedCatalogId;
            var expectedTitle = selected.CachedCatalogName;
            liveFieldResult = null;
            checkingLiveFields = true;
            liveFieldCancellation = new CancellationTokenSource();
            var cancellation = liveFieldCancellation;
            UpdateScanControls();
            Task.Run(() => {
                CatalogGame exact = null;
                string error = null;
                bool canceled = false;
                try
                {
                    exact = new WorkspaceIdentityReview(dataPath).FetchExact(igdbId,
                        credentials.ClientId, credentials.ClientSecret, cancellation.Token);
                }
                catch (OperationCanceledException) {canceled = true;}
                catch (Exception ex) {error = ex.Message;}
                window.Dispatcher.BeginInvoke(new Action(() => {
                    checkingLiveFields = false;
                    if (!window.IsVisible) return;
                    if (canceled)
                    {
                        scanStatus.Text = "IGDB field check canceled. No metadata changed.";
                    }
                    else if (error != null)
                    {
                        scanStatus.Text = "IGDB field check unavailable: " + error;
                    }
                    else
                    {
                        try
                        {
                            var current = CurrentIdentityInput(selected);
                            ConfirmUnchangedLookup(current, expectedId, expectedTitle);
                            if (current.Id != input.Id || current.MissingMask != input.MissingMask ||
                                current.LocalReleaseYear != input.LocalReleaseYear ||
                                !String.Equals(current.Name, input.Name, StringComparison.Ordinal) ||
                                !String.Equals(current.Platform, input.Platform, StringComparison.Ordinal))
                                throw new InvalidOperationException("Playnite metadata changed during IGDB lookup.");
                            liveFieldResult = WorkspaceLiveComparison.Build(current, exact,
                                igdbId, expectedTitle);
                            scanStatus.Text = "Checked IGDB fields for " + current.Name +
                                ": " + liveFieldResult.PossibleAdditions +
                                " possible addition(s). No import approved.";
                        }
                        catch (Exception ex)
                        {
                            liveFieldResult = null;
                            scanStatus.Text = "Field comparison invalidated: " + ex.Message;
                        }
                    }
                    liveFieldCancellation = null;
                    UpdateScanControls();
                }));
            });
        }
        private void ConfirmUnchangedLookup(CatalogInput live, string id, string title)
        {
            var cache = new CatalogScanCache(dataPath);
            cache.Load(); // Never overwrite or ignore malformed saved records.
            CatalogScanRecord record;
            if (!cache.TryGet(live, DateTime.UtcNow, out record) ||
                !WorkspaceIdentityRules.SameSaved(record, id, title, live))
                throw new InvalidOperationException("Saved IGDB suggestion changed. Reload before comparing fields.");
        }
        private CatalogCredentialData IdentityCredentials()
        {
            var values = new CatalogCredentialStore(dataPath).Load();
            if (String.IsNullOrWhiteSpace(values.ClientId) ||
                String.IsNullOrWhiteSpace(values.ClientSecret))
                throw new InvalidOperationException("Configure your Twitch/IGDB credentials in Settings first.");
            return values;
        }
        private static string CandidateLabel(CatalogGame value)
        {
            if (value == null) return "(no IGDB record)";
            string platforms = String.Join(", ", (value.Platforms ?? new List<string>()).Take(3));
            return (value.Name ?? "(untitled)") + "   ·   ID " + value.Id +
                (value.FirstReleaseYear > 0 ? " · " + value.FirstReleaseYear : "") +
                (platforms.Length == 0 ? " · platform not specified" : " · " + platforms);
        }
        private sealed class IdentityChoiceRow
        {
            internal CatalogGame Record;
            public string Title { get { return Record == null ? "" : Record.Name ?? ""; } }
            public string Platform { get { return Record == null || Record.Platforms == null ||
                Record.Platforms.Count == 0 ? "Not listed" : String.Join(", ", Record.Platforms); } }
            public string Year { get { return Record != null && Record.FirstReleaseYear > 0 ?
                Record.FirstReleaseYear.ToString() : "—"; } }
            public string Id { get { return Record == null ? "" : Record.Id.ToString(); } }
            public string Relevance { get; set; }
        }
        // Presentation order only. These labels are not matching evidence or
        // permission to import; the selected ID is always re-fetched by the service.
        private static int CandidateTitleRank(string wanted, string candidate)
        {
            var a = (wanted ?? "").Trim();
            var b = (candidate ?? "").Trim();
            if (a.Length == 0 || b.Length == 0) return 0;
            if (String.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return 3;
            if (b.StartsWith(a + ":", StringComparison.OrdinalIgnoreCase) ||
                b.StartsWith(a + " -", StringComparison.OrdinalIgnoreCase) ||
                b.StartsWith(a + " (", StringComparison.OrdinalIgnoreCase)) return 2;
            if (b.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            return 0;
        }
        private static bool CandidatePlatformMatch(string platform, CatalogGame game)
        {
            var expected = CatalogCore.PlatformKey(platform ?? "");
            return expected.Length > 0 && game != null && game.Platforms != null &&
                game.Platforms.Any(value => String.Equals(expected,
                    CatalogCore.PlatformKey(value ?? ""), StringComparison.Ordinal));
        }
        private static List<IdentityChoiceRow> DisplayCandidateRows(IEnumerable<CatalogGame> games,
            string title, string platform)
        {
            return (games ?? Enumerable.Empty<CatalogGame>()).Where(x => x != null && x.Id > 0)
                .Select(x => new {Value = x, Rank = CandidateTitleRank(title, x.Name),
                    Platform = CandidatePlatformMatch(platform, x)})
                .OrderByDescending(x => x.Rank).ThenByDescending(x => x.Platform)
                .ThenBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Value.Id).Select(x => new IdentityChoiceRow {
                    Record = x.Value,
                    Relevance = x.Rank == 3 && x.Platform ? "Title + platform" :
                        x.Rank == 3 ? "Check platform" :
                        x.Rank == 2 && x.Platform ? "Edition · check" :
                        x.Rank == 2 ? "Edition + platform?" :
                        x.Rank == 1 ? "Partial title" : "Other result"
                }).ToList();
        }
        private void ShowIdentitySearch()
        {
            var selected = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            if (selected != null) ShowIdentitySearch(selected.GameId, false);
        }
        private bool ShowIdentitySearch(Guid selectedGameId, bool restoreReviewMark)
        {
            if (BusyForNavigation() || selectedGameId == Guid.Empty) return false;
            var selected = items.FirstOrDefault(x => x.GameId == selectedGameId);
            if (selected == null || !selected.FromSaved || !selected.CanMark) return false;
            CatalogCredentialData credentials;
            try {CurrentIdentityInput(selected); credentials = IdentityCredentials();}
            catch (Exception ex)
            {api.Dialogs.ShowMessage(ex.Message, "IGDB identity review");return false;}
            var dialog = new Window {Title = "Choose IGDB game — " + selected.Title,
                Width = 1000, Height = 670, MinWidth = 760, MinHeight = 500, Owner = window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Background, ResizeMode = ResizeMode.CanResize};
            var root = new DockPanel {Margin = new Thickness(18)};
            var head = new StackPanel {Margin = new Thickness(0, 0, 0, 12)};
            DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
            var heading = Label("Find the correct IGDB game", 19, Text);
            heading.FontWeight = FontWeights.SemiBold;
            head.Children.Add(heading);
            var context = Label("Playnite · " + selected.Title + "   |   " + selected.Platform, 12, Muted);
            context.Margin = new Thickness(0, 3, 0, 10); head.Children.Add(context);
            var queryRow = new Grid {Margin = new Thickness(0, 0, 0, 8), Height = ToolbarHeight};
            queryRow.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
            queryRow.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            head.Children.Add(queryRow);
            // Use the existing discovery-only normalization to make filename-like
            // titles easier to search; this is never identity or rename evidence.
            var suggestedSearch = CatalogCore.DiscoveryTitle(selected.Title, selected.Platform);
            if (!WorkspaceIdentityRules.ValidSearch(suggestedSearch)) suggestedSearch = selected.Title;
            var query = new TextBox {Text = suggestedSearch, Height = ToolbarHeight, FontSize = 13,
                Padding = new Thickness(9, 0, 9, 0), VerticalContentAlignment = VerticalAlignment.Center,
                Background = Panel, Foreground = Text, BorderBrush = Stroke};
            queryRow.Children.Add(query);
            var idRow = new Grid {Margin = new Thickness(0, 0, 0, 10), Height = ToolbarHeight};
            idRow.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            idRow.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(176)});
            idRow.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            head.Children.Add(idRow);
            var exactCaption = Label("Or enter exact IGDB ID", 12, Muted);
            exactCaption.Margin = new Thickness(0, 0, 10, 0);
            idRow.Children.Add(exactCaption);
            var idField = new TextBox {Height = ToolbarHeight, FontSize = 13,
                Padding = new Thickness(9, 0, 9, 0), VerticalContentAlignment = VerticalAlignment.Center,
                Background = Panel, Foreground = Text, BorderBrush = Stroke};
            Grid.SetColumn(idField, 1); idRow.Children.Add(idField);
            var status = Label("Search by title, or check an exact ID. No search runs on opening.", 12, Muted);
            head.Children.Add(status);
            var footer = new Grid {Margin = new Thickness(0, 11, 0, 0)};
            footer.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
            footer.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
            var safety = Label("Selection saves a suggestion only · no import or identity approval", 11, Muted);
            footer.Children.Add(safety);
            var footerActions = new StackPanel {Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right};
            Grid.SetColumn(footerActions, 1); footer.Children.Add(footerActions);
            var results = new DataGrid {AutoGenerateColumns = false, IsReadOnly = true,
                CanUserAddRows = false, CanUserDeleteRows = false, SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow, RowHeight = 36, ColumnHeaderHeight = 36,
                Background = Panel, Foreground = Text, RowBackground = Panel,
                AlternatingRowBackground = AlternateRow, AlternationCount = 2,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                BorderBrush = Stroke, BorderThickness = new Thickness(1),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = SoftStroke, EnableRowVirtualization = true};
            var choiceHeaderStyle = new Style(typeof(DataGridColumnHeader));
            choiceHeaderStyle.Setters.Add(new Setter(Control.BackgroundProperty, PanelRaised));
            choiceHeaderStyle.Setters.Add(new Setter(Control.ForegroundProperty, Muted));
            choiceHeaderStyle.Setters.Add(new Setter(Control.BorderBrushProperty, SoftStroke));
            choiceHeaderStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            choiceHeaderStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 8, 5)));
            choiceHeaderStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            results.ColumnHeaderStyle = choiceHeaderStyle;
            var choiceCellStyle = new Style(typeof(DataGridCell));
            choiceCellStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 3, 10, 3)));
            choiceCellStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            choiceCellStyle.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            choiceCellStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            // WPF otherwise renders a bright keyboard-focus rectangle around each
            // clicked cell, even when the picker is configured for whole rows.
            // Keep the chosen game visually unified as one selected row.
            choiceCellStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
            choiceCellStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            choiceCellStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
            var choiceSelectedCell = new Trigger {Property = DataGridCell.IsSelectedProperty, Value = true};
            choiceSelectedCell.Setters.Add(new Setter(Control.BackgroundProperty, Selection));
            choiceSelectedCell.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
            choiceSelectedCell.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            choiceCellStyle.Triggers.Add(choiceSelectedCell);
            results.CellStyle = choiceCellStyle;
            var choiceRowStyle = new Style(typeof(DataGridRow));
            choiceRowStyle.Setters.Add(new Setter(Control.BackgroundProperty, Panel));
            var choiceAlternate = new Trigger {Property = ItemsControl.AlternationIndexProperty, Value = 1};
            choiceAlternate.Setters.Add(new Setter(Control.BackgroundProperty, AlternateRow));
            choiceRowStyle.Triggers.Add(choiceAlternate);
            var choiceSelected = new Trigger {Property = DataGridRow.IsSelectedProperty, Value = true};
            choiceSelected.Setters.Add(new Setter(Control.BackgroundProperty, Selection));
            choiceRowStyle.Triggers.Add(choiceSelected);
            results.RowStyle = choiceRowStyle;
            AddColumn(results, "IGDB game", "Title", 300);
            AddColumn(results, "Platform(s)", "Platform", 250);
            AddColumn(results, "Year", "Year", 62);
            AddColumn(results, "IGDB ID", "Id", 90);
            AddColumn(results, "Title relevance", "Relevance", 124);
            results.Columns[0].Width = new DataGridLength(2.2, DataGridLengthUnitType.Star);
            results.Columns[1].Width = new DataGridLength(1.7, DataGridLengthUnitType.Star);
            results.Columns[2].Width = new DataGridLength(68);
            results.Columns[3].Width = new DataGridLength(95);
            results.Columns[4].Width = new DataGridLength(145);
            ScrollViewer.SetHorizontalScrollBarVisibility(results, ScrollBarVisibility.Auto);
            Button find = null, use = null, exact = null;
            bool busyLookup = false;
            CancellationTokenSource active = null;
            dialog.Closed += (sender, args) => {if (active != null) active.Cancel();};
            Action<bool> lockControls = busy => {
                busyLookup = busy;
                if (find != null) find.IsEnabled = !busy;
                if (use != null) use.IsEnabled = !busy && results.SelectedItem != null;
                if (exact != null) exact.IsEnabled = !busy;
                query.IsEnabled = !busy; idField.IsEnabled = !busy;
            };
            var service = new WorkspaceIdentityReview(dataPath);
            Action<long> pick = igdbId => {
                if (busyLookup || igdbId <= 0) return;
                active = new CancellationTokenSource();
                var localToken = active.Token;
                lockControls(true);
                status.Text = "Verifying exact IGDB ID " + igdbId + " (read-only)...";
                Task.Run(() => {
                    CatalogGame fetched = null; string failure = null;
                    try {fetched = service.FetchExact(igdbId, credentials.ClientId,
                        credentials.ClientSecret, localToken);}
                    catch (OperationCanceledException) {failure = "Verification canceled.";}
                    catch (Exception ex) {failure = ex.Message;}
                    var final = fetched;var error = failure;
                    dialog.Dispatcher.BeginInvoke(new Action(() => {
                        if (!dialog.IsVisible) return;
                        lockControls(false);
                        if (error != null) {status.Text = error;return;}
                        string details = CandidateLabel(final);
                        if (api.Dialogs.ShowMessage("Use this exact IGDB record as the saved suggestion?\n\n" +
                            "Playnite: " + selected.Title + "\n" + details + "\n\n" +
                            "Check platform, game edition, demo/DLC status, and title. " +
                            "Any prior confirmation will be forgotten. No metadata will be imported.",
                            "Choose suggested IGDB identity", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                            {status.Text = "No changes saved.";return;}
                        try
                        {
                            var live = CurrentIdentityInput(selected);
                            service.StageChoice(live, selected.CachedCatalogId,
                                selected.CachedCatalogName, final);
                            markedIds.Remove(selected.GameId);
                            dialog.DialogResult = true;
                        }
                        catch (Exception ex) {status.Text = "Could not save selection: " + ex.Message;}
                    }));
                });
            };
            find = ActionButton("Search IGDB", "\uE721", () => {
                if (busyLookup) return;
                if (!WorkspaceIdentityRules.ValidSearch(query.Text))
                {status.Text = "Enter a game title of 2–120 characters.";return;}
                string term = query.Text;
                active = new CancellationTokenSource();
                var localToken = active.Token;
                lockControls(true);
                status.Text = "Searching IGDB for alternate game identities...";
                Task.Run(() => {
                    List<CatalogGame> found = null;string failure = null;
                    try {found = service.Search(term, credentials.ClientId, credentials.ClientSecret, localToken);}
                    catch (OperationCanceledException) {failure = "Search canceled.";}
                    catch (Exception ex) {failure = ex.Message;}
                    var returned = found;var error = failure;
                    dialog.Dispatcher.BeginInvoke(new Action(() => {
                        if (!dialog.IsVisible) return;
                        lockControls(false);
                        if (error != null) {status.Text = error;return;}
                        var visible = DisplayCandidateRows(returned, selected.Title, selected.Platform);
                        results.ItemsSource = visible;
                        results.SelectedItem = null; // Never preselect an unrelated fuzzy result.
                        status.Text = visible.Count + " IGDB results · sorted by title and platform relevance; not verified matches.";
                    }));
                });
            });
            find.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(find, 1); queryRow.Children.Add(find);
            exact = ActionButton("Check exact ID", "\uE8A7", () => {
                long number;
                if (!Int64.TryParse(idField.Text.Trim(), out number) || number <= 0)
                {status.Text = "Enter a positive numeric IGDB ID.";return;}
                pick(number);
            });
            exact.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(exact, 2); idRow.Children.Add(exact);
            use = ActionButton("Use highlighted suggestion", "\uE8FB", () => {
                var choice = results.SelectedItem as IdentityChoiceRow;
                if (choice != null && choice.Record != null) pick(choice.Record.Id);
            }, true);
            use.IsEnabled = false;
            results.SelectionChanged += (sender, args) => {
                if (use != null) use.IsEnabled = !busyLookup && results.SelectedItem != null;};
            footerActions.Children.Add(use);
            footerActions.Children.Add(ActionButton("Close", "\uE8BB", () => dialog.Close()));
            root.Children.Add(results);
            dialog.Content = root;
            if (dialog.ShowDialog() != true) return false;
            // StageChoice intentionally removes the old review mark because its
            // previous candidate is invalid. Restore review *intent* only after
            // a successful choice, and only for the scoped caller. This never
            // restores confirmation, strict evidence, fields or import approval.
            if (restoreReviewMark) markedIds.Add(selected.GameId);
            SafeReload();
            var current = items.FirstOrDefault(x => x.GameId == selected.GameId);
            if (current == null || !current.CanMark || !current.FromSaved ||
                NeedsManualIdentity(current) ||
                (restoreReviewMark && !markedIds.Contains(selected.GameId)))
            {
                markedIds.Remove(selected.GameId);
                scanStatus.Text = "IGDB choice saved, but the game changed. Reload before reviewing.";
                return false;
            }
            scanStatus.Text = "Saved an IGDB suggestion for " + selected.Title +
                ". Confirm the identity and check exact fields before any import.";
            return true;
        }
        private void StartConfirmIdentity()
        {
            if (scanning) return;
            var selected = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            long igdbId;
            if (selected == null || !selected.FromSaved || !selected.CanMark ||
                selected.IdentityConfirmed || !Int64.TryParse(selected.CachedCatalogId, out igdbId) ||
                igdbId <= 0 || String.IsNullOrWhiteSpace(selected.CachedCatalogName)) return;
            CatalogCredentialData credentials;
            try {CurrentIdentityInput(selected); credentials = IdentityCredentials();}
            catch (Exception ex) {api.Dialogs.ShowMessage(ex.Message, "IGDB identity review");return;}
            scanning = true;
            scanCancellation = new CancellationTokenSource();
            var token = scanCancellation.Token;
            scanStatus.Text = "Checking exact IGDB identity before manual confirmation...";
            UpdateScanControls();
            Task.Run(() => {
                CatalogGame exact = null;string failure = null;
                try {exact = new WorkspaceIdentityReview(dataPath).FetchExact(igdbId,
                    credentials.ClientId, credentials.ClientSecret, token);}
                catch (OperationCanceledException) {failure = "Identity check canceled.";}
                catch (Exception ex) {failure = ex.Message;}
                var fetched = exact;var error = failure;
                window.Dispatcher.BeginInvoke(new Action(() => {
                    scanning = false;
                    scanCancellation.Dispose();scanCancellation = null;
                    if (error != null) {scanStatus.Text = error;UpdateScanControls();return;}
                    if (fetched.Id != igdbId || !String.Equals(fetched.Name,
                        selected.CachedCatalogName, StringComparison.Ordinal))
                    {scanStatus.Text = "IGDB title changed. Refresh or correct the saved identity first.";
                        UpdateScanControls();return;}
                    if (api.Dialogs.ShowMessage("Remember this exact IGDB identity for this Playnite game?\n\n" +
                        "Playnite: " + selected.Title + " · " + selected.Platform + "\n" +
                        "IGDB: " + CandidateLabel(fetched) + "\n\n" +
                        "Verify game, platform and edition carefully. This remembers only an " +
                        "identity decision. The importer STILL performs live verification, " +
                        "field comparison and requires separate backup and final approval.",
                        "Confirm IGDB identity", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {scanStatus.Text = "No identity decision saved.";UpdateScanControls();return;}
                    try
                    {
                        var live = CurrentIdentityInput(selected);
                        new WorkspaceIdentityReview(dataPath).Confirm(live,
                            selected.CachedCatalogId, selected.CachedCatalogName, fetched);
                        SafeReload();
                        scanStatus.Text = "Identity confirmed for " + selected.Title +
                            ". No Playnite metadata changed or import authorized.";
                    }
                    catch (Exception ex) {scanStatus.Text = "Confirmation not saved: " + ex.Message;}
                    UpdateScanControls();
                }));
            });
        }
        private void ForgetFocusedIdentity()
        {
            if (scanning) return;
            var selected = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
            if (selected == null || !selected.IdentityConfirmed) return;
            if (api.Dialogs.ShowMessage("Forget the manual IGDB confirmation for " +
                selected.Title + "?\nNo game metadata or history will be changed.",
                "Forget IGDB identity", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                var live = CurrentIdentityInput(selected);
                new WorkspaceIdentityReview(dataPath).Forget(live,
                    selected.CachedCatalogId, selected.CachedCatalogName);
                markedIds.Remove(selected.GameId);
                SafeReload();
                scanStatus.Text = "Identity confirmation forgotten; game requires review again.";
            }
            catch (Exception ex) {api.Dialogs.ShowMessage("Could not forget identity: " + ex.Message,
                "IGDB identity review");}
        }

        // Same DPAPI credential store as Classic. No plaintext settings files,
        // logs, exports, or automated network testing on opening the workspace.
        private void ShowIgdbSettings()
        {
            if (scanning) return;
            CatalogCredentialData stored;
            try {stored = new CatalogCredentialStore(dataPath).Load();}
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Stored credentials could not be read: " + ex.Message,
                    "IGDB settings unavailable");
                return;
            }
            var dialog = api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton = false});
            dialog.Title = "IGDB / Twitch settings — Design C";
            dialog.Width = 650; dialog.Height = 435;
            dialog.MinWidth = 490; dialog.MinHeight = 380;
            dialog.Owner = window; dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Background = Background;
            var root = new DockPanel {Margin = new Thickness(18)};
            var form = new StackPanel(); root.Children.Add(form);
            form.Children.Add(Label("Use a Twitch Developer application's Client ID and Client Secret " +
                "for IGDB access. Stored values are encrypted for your Windows account.", 13, Muted));
            var idCaption = Label("Twitch Developer Client ID", 12, Text);
            idCaption.Margin = new Thickness(0, 14, 0, 5); form.Children.Add(idCaption);
            var clientId = new TextBox {Text = stored.ClientId ?? "", Height = ToolbarHeight,
                Background = Panel, Foreground = Text, BorderBrush = Stroke, FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center};
            form.Children.Add(clientId);
            var secretCaption = Label("Client Secret", 12, Text);
            secretCaption.Margin = new Thickness(0, 13, 0, 5); form.Children.Add(secretCaption);
            var clientSecret = new PasswordBox {Password = stored.ClientSecret ?? "", Height = ToolbarHeight,
                Background = Panel, Foreground = Text, BorderBrush = Stroke, FontSize = 13};
            form.Children.Add(clientSecret);
            var testState = Label("Not tested in this session.", 12, Muted);
            testState.Margin = new Thickness(0, 13, 0, 0); form.Children.Add(testState);
            var footer = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)};
            DockPanel.SetDock(footer, Dock.Bottom); root.Children.Insert(0, footer);
            var testToken = new CancellationTokenSource();
            dialog.Closed += (sender, args) => testToken.Cancel();
            Button test = null, save = null;
            bool testPassed = false;
            clientId.TextChanged += (sender, args) => {testPassed = false; testState.Text = "Not tested with these credentials.";};
            clientSecret.PasswordChanged += (sender, args) => {testPassed = false; testState.Text = "Not tested with these credentials.";};
            test = ActionButton("Test connection", "\uE774", () => {
                if (String.IsNullOrWhiteSpace(clientId.Text) || String.IsNullOrWhiteSpace(clientSecret.Password))
                {testState.Text = "Enter both credentials first.";return;}
                var id = clientId.Text; var secret = clientSecret.Password;
                testPassed = false;
                clientId.IsEnabled = false; clientSecret.IsEnabled = false;
                test.IsEnabled = false; save.IsEnabled = false;
                testState.Text = "Checking Twitch authentication and IGDB read access...";
                Task.Run(() => {
                    IgdbConnectionResult result = null;
                    try {result = WorkspaceCatalogLookup.TestConnection(id, secret, testToken.Token);}
                    catch (OperationCanceledException) { }
                    catch (Exception) {result = IgdbConnectionResult.BadResponse();}
                    dialog.Dispatcher.BeginInvoke(new Action(() => {
                        if (!dialog.IsVisible) return;
                        testPassed = result != null && result.Connected;
                        testState.Text = result == null ? "Connection test canceled." : result.Message;
                        testState.Foreground = testPassed ? Good : Warning;
                        clientId.IsEnabled = true; clientSecret.IsEnabled = true;
                        test.IsEnabled = true; save.IsEnabled = true;
                    }));
                });
            });
            footer.Children.Add(test);
            save = ActionButton("Save credentials", "\uE74E", () => {
                if (String.IsNullOrWhiteSpace(clientId.Text) || String.IsNullOrWhiteSpace(clientSecret.Password))
                {testState.Text = "Both credentials are required.";return;}
                try
                {
                    new CatalogCredentialStore(dataPath).Save(clientId.Text, clientSecret.Password);
                    scanStatus.Text = testPassed ? "IGDB settings saved; connection verified." :
                        "IGDB settings saved securely (not tested).";
                    dialog.DialogResult = true;
                }
                catch (Exception ex)
                {api.Dialogs.ShowErrorMessage("Could not save encrypted settings: " + ex.Message,
                    "IGDB settings");}
            }, true);
            footer.Children.Add(save);
            footer.Children.Add(ActionButton("Cancel", "\uE8BB", () => dialog.DialogResult = false));
            dialog.Content = root;
            dialog.ShowDialog();
        }

        private void UpdateScanControls()
        {
            if (scanPendingButton == null) return;
            int pending = items.Count(x => x.Group == WorkspaceTaskGroup.CheckNeeded);
            int chosenPending = items.Count(x => x.Group == WorkspaceTaskGroup.CheckNeeded &&
                markedIds.Contains(x.GameId));
            int available = markedIds.Count == 0 ? pending : chosenPending;
            // Preserve the established bounded-scan control contract; live
            // field checks disable it separately, as in prior verified builds.
            scanPendingButton.IsEnabled = !scanning && available > 0;
            if (checkingLiveFields) scanPendingButton.IsEnabled = false;
            if (scanAllPendingButton != null)
            {
                scanAllPendingButton.IsEnabled = !scanning && !checkingLiveFields && available > 0;
                scanAllPendingButton.Content = IconLabel("\uE721", "Scan all " + available +
                    " pending" + (markedIds.Count > 0 ? " (marked)" : ""), Text);
            }
            scanPendingButton.Content = IconLabel("\uE721", "Scan " + Math.Min(available,
                WorkspaceScanSelection.BatchLimit) + " pending" + (markedIds.Count > 0 ? " (marked)" : ""), Background);
            if (cancelScanButton != null)
                cancelScanButton.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
            if (settingsButton != null) settingsButton.IsEnabled = !scanning && !checkingLiveFields;
            if (reviewMarksButton != null) reviewMarksButton.IsEnabled = !scanning && !checkingLiveFields && markedIds.Count > 0;
            if (refreshMarkedButton != null)
            {
                int savedMarkedCount = items.Count(x => x.FromSaved && x.CanMark && markedIds.Contains(x.GameId));
                refreshMarkedButton.IsEnabled = !scanning && savedMarkedCount > 0;
                if (checkingLiveFields) refreshMarkedButton.IsEnabled = false;
                refreshMarkedButton.Content = IconLabel("\uE72C", "Refresh " +
                    Math.Min(savedMarkedCount, WorkspaceScanSelection.BatchLimit) + " marked saved", Text);
            }
            if (refreshFocusedButton != null)
            {
                var focused = table == null ? null : table.SelectedItem as LibraryWorkspaceItem;
                refreshFocusedButton.IsEnabled = !scanning && !checkingLiveFields && focused != null &&
                    focused.FromSaved && focused.CanMark;
            }
            if (table != null) UpdateInspector();
        }

        // A fresh bounded resolver run is the ONLY way to obtain native strict
        // evidence. Reusing a saved Strong score is never sufficient. This is
        // read-only and does not confirm an identity or approve metadata writes.
        private void VerifyFreshStrictEvidence()
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying ||
                window == null || !window.IsVisible) return;
            var selected = items.Where(x=>markedIds.Contains(x.GameId) && x.CanMark &&
                x.FromSaved && !x.IdentityConfirmed).OrderBy(x=>x.Title,
                StringComparer.OrdinalIgnoreCase).ToList();
            if (selected.Count==0)
            {
                scanStatus.Text="Mark one or more saved, unconfirmed IGDB suggestions first.";
                return;
            }
            if (selected.Count>WorkspaceReviewHandoff.MaximumSelectedGames)
            {
                scanStatus.Text="Too many marked games for one bounded strict review.";
                return;
            }
            CatalogCredentialData credentials;
            try {credentials=new CatalogCredentialStore(dataPath).Load();}
            catch(Exception ex)
            {
                api.Dialogs.ShowErrorMessage(ex.Message,"IGDB settings unavailable");return;
            }
            if (String.IsNullOrWhiteSpace(credentials.ClientId) ||
                String.IsNullOrWhiteSpace(credentials.ClientSecret))
            {ShowIgdbSettings();return;}
            var requests=new List<Tuple<CatalogInput,long,string,string>>();
            try
            {
                var cache=new CatalogScanCache(dataPath);
                cache.Load();
                foreach(var row in selected)
                {
                    Game game=api.Database.Games[row.GameId];
                    if (game==null) throw new InvalidOperationException("A selected game disappeared.");
                    var live=ScanInput(game);
                    ConfirmUnchangedLookup(live,row.CachedCatalogId,row.CachedCatalogName);
                    if (!String.Equals(row.Title,live.Name,StringComparison.Ordinal) ||
                        !String.Equals(row.Platform,live.Platform,StringComparison.Ordinal) ||
                        row.MissingMask!=live.MissingMask)
                        throw new InvalidOperationException("A selected game changed; reload first.");
                    long id;
                    if (!Int64.TryParse(row.CachedCatalogId,out id) || id<=0 ||
                        String.IsNullOrWhiteSpace(row.CachedCatalogName))
                        throw new InvalidOperationException("A selected game has no exact IGDB identity.");
                    CatalogScanRecord saved;
                    if (!cache.TryGet(live,DateTime.UtcNow,out saved) ||
                        !String.Equals(saved.ScannedUtc,row.CachedScannedUtc,StringComparison.Ordinal))
                        throw new InvalidOperationException("Saved lookup changed; reload before strict review.");
                    requests.Add(Tuple.Create(live,id,row.CachedCatalogName,row.CachedScannedUtc));
                }
            }
            catch(Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Strict review blocked: "+ex.Message,
                    "Refresh required");return;
            }
            var answer=api.Dialogs.ShowMessage("Run a fresh, independent IGDB identity check for "+
                requests.Count+" marked game(s)?\n\nThis may issue multiple bounded searches per game. " +
                "Only the existing resolver may grant temporary strict readiness; " +
                "a saved Strong label is never enough. This does not approve or import metadata.",
                "Verify fresh strict evidence (read-only)",MessageBoxButton.YesNo,MessageBoxImage.Question);
            if (answer!=MessageBoxResult.Yes) return;
            foreach(var request in requests)freshStrictEvidence.Remove(request.Item1.Id);
            scanning=true;
            scanCancellation=new CancellationTokenSource();
            var cancellation=scanCancellation;
            var token=cancellation.Token;
            UpdateScanControls();
            scanStatus.Text="Checking fresh strict evidence: 0 / "+requests.Count+". No writes.";
            Task.Run(()=>{
                var proved=new List<FreshStrictEvidence>();
                int done=0;bool canceled=false;string error=null;
                try
                {
                    WorkspaceFreshStrictLookup.Check(requests,credentials.ClientId,
                        credentials.ClientSecret,token,
                        request=>proved.Add(new FreshStrictEvidence {
                            Snapshot=request.Item1,IgdbId=request.Item2,
                            CatalogTitle=request.Item3,CacheTimestamp=request.Item4
                        }),
                        completed=>{
                            done=completed;
                            window.Dispatcher.BeginInvoke(new Action(()=>{
                                if (window.IsVisible)scanStatus.Text="Fresh identity recheck "+
                                    completed+" / "+requests.Count+". Read-only.";
                            }));
                        });
                }
                catch(OperationCanceledException){canceled=true;}
                catch(Exception ex){error=ex.Message;}
                int checkedCount=done;
                bool wasCanceled=canceled;
                string failure=error;
                window.Dispatcher.BeginInvoke(new Action(()=>{
                    scanning=false;
                    scanCancellation=null;
                    cancellation.Dispose();
                    // Recheck both Playnite and the exact saved identity before
                    // publishing a single proof from a network worker.
                    int ready=0;
                    try
                    {
                        var cache=new CatalogScanCache(dataPath);
                        cache.Load();
                        foreach(var proof in proved)
                        {
                            Game liveGame=api.Database.Games[proof.Snapshot.Id];
                            if (liveGame==null)continue;
                            var current=ScanInput(liveGame);
                            CatalogScanRecord saved;
                            if (!proof.Matches(current,proof.IgdbId.ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                                proof.CatalogTitle,proof.CacheTimestamp) ||
                                !cache.TryGet(current,DateTime.UtcNow,out saved) ||
                                !String.Equals(saved.CatalogId,proof.IgdbId.ToString(
                                    System.Globalization.CultureInfo.InvariantCulture),StringComparison.Ordinal) ||
                                !String.Equals(saved.Candidate,proof.CatalogTitle,StringComparison.Ordinal) ||
                                !String.Equals(saved.ScannedUtc,proof.CacheTimestamp,StringComparison.Ordinal))
                                continue;
                            freshStrictEvidence[current.Id]=proof;
                            ready++;
                        }
                        SafeReload();
                    }
                    catch(Exception ex)
                    {
                        freshStrictEvidence.Clear();
                        SafeReload();
                        scanStatus.Text="Strict evidence could not be verified: "+ex.Message;
                        UpdateScanControls();return;
                    }
                    scanStatus.Text=wasCanceled?"Strict review canceled: "+ready+
                        " eligible of "+checkedCount+" checked; no import approved.":
                        failure!=null?"Strict review stopped after "+checkedCount+
                        ": "+failure+". "+ready+" eligible; no import approved.":
                        "Fresh strict check: "+ready+" / "+requests.Count+
                        " eligible for live preflight. Review and approve separately.";
                    UpdateScanControls();
                }));
            });
        }

        // Explicit, opt-in full rescan. Archive the entire lookup cache before
        // rebuilding the pending queue; never delete confirmations, ignored
        // decisions, metadata journals, media staging or Playnite game records.
        private void StartFullRescan()
        {
            if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying ||
                window == null || !window.IsVisible) return;
            try
            {
                // Fail closed on malformed cached data, before any file move.
                var cache = new CatalogScanCache(dataPath);
                cache.Load();
                int saved = items.Count(x => x.FromSaved);
                var answer = api.Dialogs.ShowMessage(
                    "Archive all saved IGDB lookup results and make eligible games pending again?\n\n" +
                    "This is an explicit full rescan reset, not an IGDB request or metadata import. " +
                    "Approximately " + saved + " currently displayed games have reusable lookup results.\n\n" +
                    "Saved lookup data is archived for recovery; no Playnite metadata, " +
                    "game/ROM files, paths, confirmed identities, ignored decisions, " +
                    "import history, or undo/recovery journals are changed.\n\n" +
                    "Temporary marked selections, staged drafts, and read-only field checks " +
                    "will be cleared. You can start Scan all pending afterward.",
                    "Confirm full IGDB rescan", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes) return;
                string archived = cache.ArchiveAndClear(DateTime.UtcNow);
                markedIds.Clear();
                draftStates.Clear();
                freshStrictEvidence.Clear();
                liveFieldResult = null;
                SafeReload();
                scanStatus.Text = "Saved IGDB lookup cache reset" +
                    (String.IsNullOrWhiteSpace(archived) ? " (no prior file). " :
                    " (previous cache archived). ") +
                    "Use Scan all pending to refresh suggestions. No metadata changed.";
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage(
                    "Full rescan reset stopped: " + ex.Message +
                    "\nSaved data was not deliberately deleted. Do not remove recovery files manually.",
                    "Could not archive IGDB lookup cache");
            }
        }

        private void StartDirectScan()
        {
            StartDirectScan(false);
        }

        // No artificial 25-game click limit: the full queue is processed in
        // 25-game read-only, independently checkpointed requests, not one huge
        // provider request. A stopped scan retains earlier completed results.
        private void StartDirectScan(bool allPending)
        {
            if (scanning || checkingLiveFields || table == null) return;
            CatalogCredentialData credentials;
            try {credentials = new CatalogCredentialStore(dataPath).Load();}
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Could not load IGDB credentials: " + ex.Message,
                    "IGDB settings"); return;
            }
            if (String.IsNullOrWhiteSpace(credentials.ClientId) ||
                String.IsNullOrWhiteSpace(credentials.ClientSecret))
            {
                ShowIgdbSettings();
                return; // Never start an unannounced scan after opening settings.
            }
            var pending = items.Where(x => x.Group == WorkspaceTaskGroup.CheckNeeded)
                // Series-only gaps remain scannable, after higher-value work.
                .OrderBy(x => CatalogEnrichmentNeeds.Priority(x.MissingMask))
                .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.GameId).ToList();
            var selected = WorkspaceScanSelection.Choose(pending, markedIds);
            if (allPending)
                selected = markedIds.Count == 0 ? pending :
                    pending.Where(x => markedIds.Contains(x)).ToList();
            if (selected.Count == 0)
            {
                scanStatus.Text = markedIds.Count > 0 ? "None of your marked games need an initial scan." :
                    "No new games require IGDB scanning.";
                return;
            }
            var inputs = new List<CatalogInput>();
            foreach (var id in selected)
            {
                Game game;
                if (!knownGames.TryGetValue(id, out game) || game == null) continue;
                var latest = api.Database.Games[id];
                if (latest == null || !String.Equals(latest.Name, game.Name, StringComparison.Ordinal))
                {scanStatus.Text = "Library changed. Reload (F5) before scanning.";return;}
                var originalPlatform = game.Platforms == null ? "" :
                    String.Join(", ", game.Platforms.Select(x => x.Name));
                var live = ScanInput(latest);
                var prior = ScanInput(game);
                if (!String.Equals(originalPlatform, live.Platform, StringComparison.Ordinal) ||
                    live.MissingCount <= 0 || live.MissingMask != prior.MissingMask ||
                    live.LocalReleaseYear != prior.LocalReleaseYear)
                {scanStatus.Text = "Library metadata changed. Reload (F5) before scanning.";return;}
                inputs.Add(live);
            }
            if (inputs.Count != selected.Count)
            {scanStatus.Text = "Selection changed. Reload (F5) before scanning.";return;}
            foreach(var input in inputs) freshStrictEvidence.Remove(input.Id);
            scanning = true;
            scanCancellation = new CancellationTokenSource();
            var token = scanCancellation.Token;
            scanStatus.Text = "Searching IGDB: 0 / " + inputs.Count + ". Metadata remains unchanged.";
            UpdateScanControls();
            Task.Run(() => {
                int completed = 0;
                bool canceled = false;
                string failure = null;
                try
                {
                    // WorkspaceCatalogLookup.Run keeps its original bounded rate-limit,
                    // per-batch cache checkpoint and cancellation behavior.
                    for (int offset = 0; offset < inputs.Count; offset += WorkspaceScanSelection.BatchLimit)
                    {
                        token.ThrowIfCancellationRequested();
                        var chunk = inputs.Skip(offset).Take(WorkspaceScanSelection.BatchLimit).ToList();
                        int earlier = completed;
                        var result = new WorkspaceCatalogLookup(dataPath).Run(chunk,
                            credentials.ClientId, credentials.ClientSecret, token, (done, total) => {
                                int progress = earlier + done;
                                window.Dispatcher.BeginInvoke(new Action(() => {
                                    if (window.IsVisible) scanStatus.Text = "IGDB scan " + progress +
                                        " / " + inputs.Count + ". Saved lookup results only; no metadata changes.";
                                }));
                            });
                        if (result == null || result.Completed < 0 || result.Completed > chunk.Count)
                            throw new InvalidOperationException("An IGDB scan checkpoint returned an invalid result.");
                        completed += result.Completed;
                        if (!String.IsNullOrWhiteSpace(result.Error))
                        {failure = result.Error; break;}
                        if (result.Canceled)
                        {canceled = true; break;}
                        if (result.Completed != chunk.Count)
                        {failure = "An IGDB lookup batch ended without a completed checkpoint."; break;}
                    }
                }
                catch (OperationCanceledException) {canceled = true;}
                catch (Exception ex) {failure = ex.Message;}
                int finished = completed;
                bool wasCanceled = canceled;
                string stoppedReason = failure;
                window.Dispatcher.BeginInvoke(new Action(() => {
                    scanning = false;
                    scanCancellation.Dispose(); scanCancellation = null;
                    // Reload shared cache and journal only after the worker ends.
                    SafeReload();
                    if (stoppedReason != null)
                        scanStatus.Text = "Scan stopped after " + finished + " / " + inputs.Count +
                            " games: " + stoppedReason;
                    else if (wasCanceled)
                        scanStatus.Text = "Scan canceled after " + finished + " / " + inputs.Count +
                            " games. Completed results retained.";
                    else scanStatus.Text = "Scanned " + finished + " games. Review suggestions before import.";
                    UpdateScanControls();
                }));
            });
        }

        // Explicit live recheck of ONE focused saved result or a marked, bounded subset.
        // No import, identity confirmation, journal update or automatic batch approval.
        private void StartSavedRefresh(bool markedBatch)
        {
            if (scanning || checkingLiveFields || table == null) return;
            var focused = table.SelectedItem as LibraryWorkspaceItem;
            var savedIds = items.Where(x => x.FromSaved && x.CanMark)
                .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.GameId).ToList();
            var selected = markedBatch ? WorkspaceRefreshPolicy.ChooseMarked(savedIds, markedIds) :
                (focused != null && focused.FromSaved && focused.CanMark ?
                    new List<Guid> {focused.GameId} : new List<Guid>());
            if (selected.Count == 0)
            {
                scanStatus.Text = "Choose a saved IGDB result, or mark saved games for refresh.";
                return;
            }
            CatalogCredentialData credentials;
            try {credentials = new CatalogCredentialStore(dataPath).Load();}
            catch (Exception ex)
            {api.Dialogs.ShowErrorMessage("Could not load IGDB credentials: " + ex.Message,
                "IGDB settings");return;}
            if (String.IsNullOrWhiteSpace(credentials.ClientId) ||
                String.IsNullOrWhiteSpace(credentials.ClientSecret))
            {ShowIgdbSettings();return;}
            var oldCache = new CatalogScanCache(dataPath);
            try {oldCache.Load();}
            catch (Exception ex)
            {api.Dialogs.ShowErrorMessage("Saved scan cache could not be read safely: " + ex.Message,
                "IGDB refresh");return;}
            var inputs = new List<CatalogInput>();
            var previous = new Dictionary<Guid, CatalogScanRecord>();
            foreach (var id in selected)
            {
                Game game;
                if (!knownGames.TryGetValue(id, out game) || game == null) continue;
                var latest = api.Database.Games[id];
                if (latest == null || !String.Equals(game.Name, latest.Name, StringComparison.Ordinal))
                {scanStatus.Text = "The Playnite library changed. Reload (F5) first.";return;}
                var before = ScanInput(game);
                var live = ScanInput(latest);
                if (!String.Equals(live.Name, before.Name, StringComparison.Ordinal) ||
                    !String.Equals(live.Platform, before.Platform, StringComparison.Ordinal) ||
                    live.MissingMask != before.MissingMask ||
                    live.LocalReleaseYear != before.LocalReleaseYear || live.MissingCount <= 0)
                {scanStatus.Text = "Metadata changed. Reload (F5) before refreshing IGDB.";return;}
                CatalogScanRecord cached;
                if (!oldCache.TryGet(live, DateTime.UtcNow, out cached))
                {scanStatus.Text = "A saved match is no longer valid. Reload (F5) first.";return;}
                inputs.Add(live);previous.Add(id, cached);
            }
            if (inputs.Count != selected.Count || inputs.Count == 0)
            {scanStatus.Text = "Selection changed. Reload (F5) before refreshing IGDB.";return;}
            foreach(var input in inputs) freshStrictEvidence.Remove(input.Id);
            scanning = true;
            scanCancellation = new CancellationTokenSource();
            var token = scanCancellation.Token;
            scanStatus.Text = "Refreshing IGDB evidence for " + inputs.Count + " game(s). No import approval.";
            UpdateScanControls();
            Task.Run(() => {
                WorkspaceScanOutcome outcome;
                try
                {
                    outcome = new WorkspaceCatalogLookup(dataPath).Run(inputs,
                        credentials.ClientId, credentials.ClientSecret, token, (done, total) => {
                            window.Dispatcher.BeginInvoke(new Action(() => {
                                if (window.IsVisible) scanStatus.Text = "Refreshing " + done + " / " + total +
                                    " · saved suggestions only";
                            }));
                        });
                }
                catch (Exception ex) {outcome = new WorkspaceScanOutcome {Error = ex.Message};}
                var result = outcome;
                window.Dispatcher.BeginInvoke(new Action(() => {
                    scanning = false;
                    scanCancellation.Dispose(); scanCancellation = null;
                    int changed = 0;
                    try
                    {
                        var current = new CatalogScanCache(dataPath);
                        current.Load();
                        foreach (var input in inputs.Take(result.Completed))
                        {
                            CatalogScanRecord refreshed;
                            if (!current.TryGet(input, DateTime.UtcNow, out refreshed) ||
                                !WorkspaceRefreshPolicy.SameIdentity(previous[input.Id].CatalogId,
                                    previous[input.Id].Candidate, refreshed.CatalogId,
                                    refreshed.Candidate))
                            {
                                if (markedIds.Remove(input.Id)) changed++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        foreach (var input in inputs) markedIds.Remove(input.Id);
                        scanStatus.Text = "Could not safely read refreshed results: " + ex.Message;
                        SafeReload();UpdateScanControls();return;
                    }
                    SafeReload();
                    if (!String.IsNullOrWhiteSpace(result.Error))
                        scanStatus.Text = "IGDB refresh stopped after " + result.Completed +
                            " game(s): " + result.Error;
                    else if (result.Canceled)
                        scanStatus.Text = "IGDB refresh canceled after " + result.Completed +
                            " game(s). Completed lookups retained.";
                    else scanStatus.Text = "Refreshed " + result.Completed + " saved result(s). " +
                        changed + " changed identities unmarked. Live lookup is not import approval.";
                    UpdateScanControls();
                }));
            });
        }

        // All long-running workspace activities must finish (or cancel at their
        // supported boundary) before transferring control to Classic.  The WPF
        // Closing handler also uses this predicate; a rejected Close must never
        // be followed by launching another matcher over the active workspace.
        private bool BusyForNavigation()
        {
            return scanning || checkingLiveFields || checkingBatchFields || nativeApplying;
        }
        private void OpenClassic()
        {
            // Keep one modal window open, and delegate live actions to the
            // existing workflow. An in-flight import or field check must never
            // cause a canceled Close followed by a second matcher window.
            if (BusyForNavigation())
            {
                api.Dialogs.ShowMessage("Finish or cancel the active scan, field check, or import " +
                    "before opening Classic. No metadata or review marks were changed.",
                    "Switch to Classic unavailable");
                return;
            }
            if (markedIds.Count > 0)
            {
                var answer = api.Dialogs.ShowMessage(
                    "This opens Classic without the " + markedIds.Count + " marked game(s). " +
                    "Use Review marked games to transfer your selection instead. Continue without it?",
                    "Open Classic without marks?", System.Windows.MessageBoxButton.YesNo);
                if (answer != System.Windows.MessageBoxResult.Yes) return;
            }
            window.Close();
            if (window.IsVisible) return; // Closing may have been canceled.
            new CatalogWindow(api, dataPath).Show();
        }
        private static void AddColumn(DataGrid grid, string header, string property, double width)
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
            style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, new Binding(property)));
            grid.Columns.Add(new DataGridTextColumn {
                Header = header, Binding = new Binding(property), Width = width,
                ElementStyle = style, IsReadOnly = true});
        }
        internal void Show()
        {
            window = api.Dialogs.CreateWindow(new WindowCreationOptions {ShowMinimizeButton = false});
            window.Title = "Smart Metadata Matcher — " + PreviewLabel;
            window.Width = 1500; window.Height = 900;
            window.MinWidth = 1080; window.MinHeight = 690;
            var owner = api.Dialogs.GetCurrentAppWindow();
            if (owner != null && owner != window) window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Background = Background;
            window.Closing += (sender, args) => {
                if (!BusyForNavigation()) return;
                args.Cancel = true;
                if (scanCancellation != null) scanCancellation.Cancel();
                if (batchFieldCancellation != null) batchFieldCancellation.Cancel();
                if (nativeImportCancellation != null) nativeImportCancellation.Cancel();
                if (liveFieldCancellation != null) liveFieldCancellation.Cancel();
                if (scanStatus != null) scanStatus.Text = "Stopping IGDB request at the next safe boundary...";
            };
            var root = new DockPanel {Margin = new Thickness(13)};

            // Compact, single-row navigation inspired by the approved UX concept.
            // The existing scope variable is an internal filter state, not a second UI control.
            var header = new StackPanel {Margin = new Thickness(0, 0, 0, 8)};
            DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
            var brand = new Grid {Margin = new Thickness(0, 0, 0, 9)};
            brand.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
            brand.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            header.Children.Add(brand);
            var identity = new StackPanel {Orientation = Orientation.Horizontal};
            identity.Children.Add(Symbol("\uE7FC", Accent, 31));
            var identityText = new StackPanel();
            var name = Label("Smart Metadata Matcher", 23, Text);
            name.FontWeight = FontWeights.SemiBold;
            identityText.Children.Add(name);
            identityText.Children.Add(Label("Scan · Review · Apply · Undo   |   Every import requires approval", 12, Muted));
            identity.Children.Add(identityText);
            brand.Children.Add(identity);
            var trailing = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center};
            settingsButton = ActionButton("IGDB settings", "\uE713", ShowIgdbSettings);
            settingsButton.ToolTip = "Twitch Client ID / Secret, encrypted storage and connection test";
            trailing.Children.Add(settingsButton);
            trailing.Children.Add(ActionButton("History", "\uE81C", ShowHistoryPreview));
            // Advanced maintenance stays discoverable without crowding the scan row.
            Button advancedButton = null;
            advancedButton = ActionButton("Advanced", "\uE712", () => {
                var menu = new ContextMenu {PlacementTarget = advancedButton,
                    Placement = PlacementMode.Bottom};
                var rescan = new MenuItem {Header = "Rescan all IGDB matches (archive saved results)..."};
                rescan.Click += (sender, args) => StartFullRescan();
                menu.Items.Add(rescan);
                var strict = new MenuItem {Header = "Verify fresh strict readiness for marked games (read-only)..."};
                strict.Click += (sender,args) => VerifyFreshStrictEvidence();
                menu.Items.Add(strict);
                var recoveryItem = new MenuItem {Header="Inspect recovery ledgers (read-only)..."};
                recoveryItem.Click += (sender,args) => ShowWorkspaceRecoveryStatus();
                menu.Items.Add(recoveryItem);
                var diagnostics = new MenuItem {Header="Export library diagnostics (CSV, read-only)..."};
                diagnostics.Click += (sender,args) => {
                    try {DiagnosticExport.Run(api);}
                    catch(Exception ex) {api.Dialogs.ShowErrorMessage("CSV export failed: "+ex.Message,
                        "Diagnostic export");}
                };
                menu.Items.Add(diagnostics);
                var shortcuts = new MenuItem {Header="Keyboard and review shortcuts..."};
                shortcuts.Click += (sender,args) => api.Dialogs.ShowMessage(
                    "Ctrl+F: search library\nF5: refresh library\nCtrl+M: mark highlighted\n" +
                    "Ctrl+Enter: review focused game or find missing IGDB match\n" +
                    "Double-click game row: review, find missing IGDB match, or view details\n" +
                    "Double-click Mark checkbox: no review opens\n" +
                    "Esc: clear focused search\nCtrl/Shift-click: select History operations\n" +
                    "Right-click a game: details, review, compare, draft or ignore.\n" +
                    "Bulk actions: mark/unmark shown, ignore/restore highlighted.\n" +
                    "Marking and scanning never authorize metadata writes.",
                    "Workspace shortcuts");
                menu.Items.Add(shortcuts);
                menu.IsOpen = true;
            });
            advancedButton.ToolTip = "Maintenance tools; explicit full rescan archives the prior lookup cache";
            trailing.Children.Add(advancedButton);
            var openClassicButton = ActionButton("Open Classic Matcher", "\uE8B7", OpenClassic);
            openClassicButton.ToolTip = "Open the original matcher for IGDB checks, manual reviews and approved updates";
            trailing.Children.Add(openClassicButton);
            Grid.SetColumn(trailing, 1); brand.Children.Add(trailing);

            scope = new ComboBox();
            foreach (var label in new[] {"Suggestions", "Needs review", "To check", "Updated", "Other", "All games"})
                scope.Items.Add(label);
            var nav = new UniformGrid {Columns = 6, Margin = new Thickness(0, 0, 0, 8)};
            nav.Children.Add(NavigationTab("Suggestions", Accent, 0, out metricSuggestions));
            nav.Children.Add(NavigationTab("Needs review", Warning, 1, out metricChoices));
            nav.Children.Add(NavigationTab("To check", Muted, 2, out metricPending));
            nav.Children.Add(NavigationTab("Updated", Good, 3, out metricUpdated));
            nav.Children.Add(NavigationTab("Other", Muted, 4, out metricOther));
            nav.Children.Add(NavigationTab("All games", Text, 5, out metricAll));
            header.Children.Add(nav);
            totals = Label("Loading library...", 12, Muted);
            totals.Margin = new Thickness(3, 0, 0, 5);
            header.Children.Add(totals);
            noticeFrame = Frame(Panel, SoftStroke, new Thickness(9, 6, 9, 6));
            noticeFrame.Margin = new Thickness(0, 0, 0, 8);
            notice = Label("Scanning saves only local IGDB lookup results. Metadata imports still require approval.", 11, Muted);
            noticeFrame.Child = notice;
            // The scanner has one concise status line instead of a second warning
            // banner. Credentials live with other utilities in the header.
            noticeFrame.Visibility = Visibility.Collapsed;
            var scanBar = new Grid {Margin = new Thickness(0, 0, 0, 9)};
            scanBar.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            scanBar.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
            var scanActions = new StackPanel {Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center};
            Grid.SetColumn(scanActions, 0); scanBar.Children.Add(scanActions);
            scanPendingButton = ActionButton("Scan next 25 pending", "\uE721", StartDirectScan, true);
            scanPendingButton.ToolTip = "Read-only IGDB lookup; stores scan-cache results. " +
                "If any games are marked, only marked pending games are scanned.";
            scanActions.Children.Add(scanPendingButton);
            scanAllPendingButton = ActionButton("Scan all pending", "\uE721", () => StartDirectScan(true));
            scanAllPendingButton.ToolTip = "Scan every pending game (or only marked pending games) " +
                "in cancellable 25-game IGDB lookup batches. Completed results are saved " +
                "after each batch; only lookup-cache data changes.";
            scanActions.Children.Add(scanAllPendingButton);
            refreshMarkedButton = ActionButton("Refresh marked saved", "\uE72C", () => StartSavedRefresh(true));
            refreshMarkedButton.ToolTip = "Recheck marked saved IGDB results only (maximum 25). " +
                "Changed identities are unmarked; never an import approval.";
            scanActions.Children.Add(refreshMarkedButton);
            cancelScanButton = ActionButton("Stop scan", "\uE71A", () => {
                if (scanCancellation != null) scanCancellation.Cancel();
            });
            cancelScanButton.Visibility = Visibility.Collapsed;
            scanActions.Children.Add(cancelScanButton);
            scanStatus = Label("Scans save suggestions only · no Playnite metadata changes", 11, Muted);
            scanStatus.Margin = new Thickness(12, 0, 0, 0);
            scanStatus.HorizontalAlignment = HorizontalAlignment.Right;
            scanStatus.TextAlignment = TextAlignment.Right;
            Grid.SetColumn(scanStatus, 1); scanBar.Children.Add(scanStatus);
            header.Children.Add(scanBar);
            // A shared 36-DIP control baseline. Columns own all inter-control
            // spacing; there are no overlapping button margins or WrapPanel breaks.
            // Search expands while filters and utilities stay at stable widths.
            var filterRow = new Grid {Height = ToolbarHeight,
                Margin = new Thickness(0, 0, 0, 12)};
            filterRow.ColumnDefinitions.Add(new ColumnDefinition {
                Width = new GridLength(1, GridUnitType.Star), MinWidth = 200});
            filterRow.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            filterRow.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            filterRow.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            header.Children.Add(filterRow);

            var searchHost = new Grid {Height = ToolbarHeight,
                Margin = new Thickness(0, 0, ToolbarGroupGap, 0)};
            search = new TextBox {Height = ToolbarHeight, FontSize = 13,
                Padding = new Thickness(34, 0, 36, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Panel, Foreground = Text, BorderBrush = Stroke,
                ToolTip = "Search games, platforms and catalog suggestions (Ctrl+F)"};
            System.Windows.Automation.AutomationProperties.SetName(search, "Search games");
            searchHost.Children.Add(search);
            var searchGlyph = Symbol("\uE721", Accent, 15);
            searchGlyph.Margin = new Thickness(11, 0, 0, 0);
            searchGlyph.HorizontalAlignment = HorizontalAlignment.Left;
            searchGlyph.IsHitTestVisible = false;
            searchHost.Children.Add(searchGlyph);
            searchPlaceholder = Label("Search games", 13, Muted);
            searchPlaceholder.Margin = new Thickness(34, 0, 32, 0);
            searchPlaceholder.IsHitTestVisible = false;
            searchHost.Children.Add(searchPlaceholder);
            searchClearButton = new Button {
                Content = new TextBlock {Text = "\uE8BB",
                    FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 13,
                    Foreground = Muted, VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center},
                Width = 28, Height = 28, Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0), Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), ToolTip = "Clear search",
                Visibility = Visibility.Collapsed};
            System.Windows.Automation.AutomationProperties.SetName(searchClearButton, "Clear search");
            searchClearButton.Click += (sender, args) => {search.Clear(); search.Focus();};
            searchHost.Children.Add(searchClearButton);
            filterRow.Children.Add(searchHost);

            platformFilter = new ComboBox {Width = 180, Height = ToolbarHeight, FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Panel, Foreground = Text, BorderBrush = Stroke,
                Margin = new Thickness(0, 0, ToolbarGap, 0)};
            System.Windows.Automation.AutomationProperties.SetName(platformFilter, "Filter by platform");
            platformFilter.ToolTip = "Platforms and Playnite sources (Steam, Epic, emulators, etc.). " +
                "Source filters do not affect IGDB platform identity.";
            platformFilter.Items.Add("All platforms"); platformFilter.SelectedIndex = 0;
            Grid.SetColumn(platformFilter, 1); filterRow.Children.Add(platformFilter);
            missingFilter = new ComboBox {Width = 180, Height = ToolbarHeight, FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Panel, Foreground = Text, BorderBrush = Stroke,
                Margin = new Thickness(0, 0, ToolbarGroupGap, 0)};
            System.Windows.Automation.AutomationProperties.SetName(missingFilter, "Filter by missing metadata");
            foreach (var label in new[] {"All missing fields", "Artwork missing", "Description missing",
                "Other fields missing", "Meaningful gaps (not Series-only)", "Series-only gaps"})
                missingFilter.Items.Add(label);
            missingFilter.SelectedIndex = 0;
            Grid.SetColumn(missingFilter, 2); filterRow.Children.Add(missingFilter);

            var utilities = new StackPanel {Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center};
            var separator = new Border {Width = 1, Height = 24, Background = SoftStroke,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, ToolbarGroupGap, 0)};
            utilities.Children.Add(separator);
            toolbarResetButton = CompactToolbarButton("Reset", "\uE777", () => {
                onlyIgnoredView = false;
                search.Clear(); platformFilter.SelectedIndex = 0; missingFilter.SelectedIndex = 0;
                ApplyFilter();
            }, 104, "Clear search and reset platform / missing-field filters");
            utilities.Children.Add(toolbarResetButton);
            var reloadButton = CompactToolbarButton("Reload", "\uE72C", SafeReload,
                104, "Reload the local Playnite library (F5); no IGDB scan or metadata writes");
            reloadButton.Margin = new Thickness(ToolbarGap, 0, 0, 0);
            utilities.Children.Add(reloadButton);
            Grid.SetColumn(utilities, 3); filterRow.Children.Add(utilities);
            UpdateToolbarActions();

            var columns = new Grid();
            columns.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(64, GridUnitType.Star), MinWidth = 540});
            columns.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(8)});
            columns.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(36, GridUnitType.Star), MinWidth = 340});
            root.Children.Add(columns);

            var listPanel = Frame(Panel, Stroke, new Thickness(0));
            Grid.SetColumn(listPanel, 0); columns.Children.Add(listPanel);
            var listLayout = new DockPanel(); listPanel.Child = listLayout;
            var listHeading = new StackPanel {Orientation = Orientation.Horizontal,
                Margin = new Thickness(12, 9, 8, 4)};
            DockPanel.SetDock(listHeading, Dock.Top); listLayout.Children.Add(listHeading);
            listHeading.Children.Add(Symbol("\uE80F", Accent, 14));
            var listCaption = Label("LIBRARY GAMES", 12, Text);
            listCaption.FontWeight = FontWeights.SemiBold;
            listHeading.Children.Add(listCaption);
            var summaryPanel = new StackPanel {Margin = new Thickness(12, 0, 10, 7)};
            DockPanel.SetDock(summaryPanel, Dock.Top); listLayout.Children.Add(summaryPanel);
            listSummary = Label("Loading games...", 12, Muted);
            summaryPanel.Children.Add(listSummary);
            // Contextual mark actions live in one bottom tray; highlight is never approval.
            var tray = Frame(PanelRaised, SoftStroke, new Thickness(9, 7, 7, 6));
            tray.Margin = new Thickness(8, 7, 8, 8);
            DockPanel.SetDock(tray, Dock.Bottom); listLayout.Children.Add(tray);
            var trayBody = new StackPanel(); tray.Child = trayBody;
            markTraySummary = Label("0 marked · 0 highlighted", 12, Text);
            trayBody.Children.Add(markTraySummary);
            var trayActions = new WrapPanel {Margin = new Thickness(0, 6, 0, 0)};
            trayBody.Children.Add(trayActions);
            // Give the selected-set review its own visual priority. It is never
            // automatic import consent; the existing approval flow remains.
            reviewMarksButton = ActionButton("Review marked games", "\uE8A7", ShowMarkedPreview, true);
            reviewMarksButton.ToolTip = "Review only marked games. Exact IGDB checks and separately " +
                "approved imports can finish in Design C; Classic remains a fallback.";
            System.Windows.Automation.AutomationProperties.SetName(reviewMarksButton, "Review marked games");
            trayActions.Children.Add(reviewMarksButton);
            markHighlightedButton = ActionButton("Mark highlighted", "\uE73E", () => MarkHighlighted(true));
            markHighlightedButton.ToolTip = "Mark games for review only; not an import approval";
            trayActions.Children.Add(markHighlightedButton);
            unmarkHighlightedButton = ActionButton("Unmark highlighted", "\uE711", () => MarkHighlighted(false));
            trayActions.Children.Add(unmarkHighlightedButton);
            clearMarkedButton = ActionButton("Clear marks", "\uE8BB", ClearMarks);
            trayActions.Children.Add(clearMarkedButton);
            // Group less-common bulk actions in one menu, not four extra toolbar buttons.
            bulkActionsButton = null;
            bulkActionsButton = ActionButton("Bulk actions", "\uE8A5", () => {
                if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying) return;
                var menu=new ContextMenu {PlacementTarget=bulkActionsButton,
                    Placement=PlacementMode.Bottom};
                var markShown=new MenuItem {Header="Mark all shown eligible games"};
                markShown.Click+=(sender,args)=>MarkShown(true);
                menu.Items.Add(markShown);
                var unmarkShown=new MenuItem {Header="Unmark shown games"};
                unmarkShown.Click+=(sender,args)=>MarkShown(false);
                menu.Items.Add(unmarkShown);
                menu.Items.Add(new Separator());
                var ignoreSelected=new MenuItem {Header="Ignore highlighted games..."};
                ignoreSelected.Click+=(sender,args)=>SetHighlightedIgnored(true);
                menu.Items.Add(ignoreSelected);
                var restoreSelected=new MenuItem {Header="Restore highlighted ignored games..."};
                restoreSelected.Click+=(sender,args)=>SetHighlightedIgnored(false);
                menu.Items.Add(restoreSelected);
                menu.Items.Add(new Separator());
                var showIgnored=new MenuItem {Header="Show ignored games (" +
                    items.Count(x=>x.Group==WorkspaceTaskGroup.Ignored) + ")"};
                showIgnored.Click+=(sender,args)=>ShowIgnoredGames();
                menu.Items.Add(showIgnored);
                var visible=table.Items.Cast<object>().OfType<LibraryWorkspaceItem>().ToList();
                var highlighted=Highlighted();
                markShown.IsEnabled=visible.Any(x=>x.CanMark && !x.Marked);
                unmarkShown.IsEnabled=visible.Any(x=>x.CanMark && x.Marked);
                ignoreSelected.IsEnabled=highlighted.Count>0 && highlighted.All(x=>
                    x.Group!=WorkspaceTaskGroup.Ignored &&
                    x.Group!=WorkspaceTaskGroup.Updated &&
                    x.Group!=WorkspaceTaskGroup.NeedsRecovery);
                restoreSelected.IsEnabled=highlighted.Count>0 && highlighted.All(x=>
                    x.Group==WorkspaceTaskGroup.Ignored);
                menu.IsOpen=true;
            });
            bulkActionsButton.ToolTip="Mark this filtered view, or ignore/restore highlighted games. No metadata writes.";
            trayActions.Children.Add(bulkActionsButton);
            selectionSummary = Label("", 11, Muted);
            selectionSummary.Visibility = Visibility.Collapsed; // Named for existing update gate.
            table = new DataGrid {
                AutoGenerateColumns = false, IsReadOnly = true,
                CanUserAddRows = false, CanUserDeleteRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionMode = DataGridSelectionMode.Extended,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                RowHeight = 39, ColumnHeaderHeight = 38,
                Background = Panel, AlternationCount = 2,
                Foreground = Text, BorderBrush = SoftStroke, BorderThickness = new Thickness(0),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = SoftStroke,
                EnableRowVirtualization = true, EnableColumnVirtualization = true,
                FontSize = 13
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(table, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(table, ScrollBarVisibility.Auto);
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, PanelRaised));
            headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, Muted));
            headerStyle.Setters.Add(new Setter(Control.BorderBrushProperty, SoftStroke));
            headerStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 7, 5)));
            headerStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            table.ColumnHeaderStyle = headerStyle;
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            cellStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            cellStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            cellStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 2, 12, 2)));
            // A Playnite theme can supply light selected-cell brushes. Keep the
            // cell transparent so the deliberate dark row selection is visible.
            var chosenCell = new Trigger {Property = DataGridCell.IsSelectedProperty, Value = true};
            chosenCell.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            chosenCell.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            cellStyle.Triggers.Add(chosenCell);
            table.CellStyle = cellStyle;
            var rowStyle = new Style(typeof(DataGridRow));
            // Explicit styles defend against WPF/Playnite theme defaults that
            // paint every second row WHITE when no base row brush is supplied.
            rowStyle.Setters.Add(new Setter(Control.BackgroundProperty, Panel));
            rowStyle.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            rowStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            var alternate = new Trigger {Property = ItemsControl.AlternationIndexProperty, Value = 1};
            alternate.Setters.Add(new Setter(Control.BackgroundProperty, AlternateRow));
            rowStyle.Triggers.Add(alternate);
            // Selection must take priority over alternating-row presentation.
            var chosen = new Trigger {Property = DataGridRow.IsSelectedProperty, Value = true};
            chosen.Setters.Add(new Setter(Control.BackgroundProperty, Selection));
            chosen.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            rowStyle.Triggers.Add(chosen);
            table.RowStyle = rowStyle;
            var checkedColumn = new DataGridTemplateColumn {Header = "Mark", Width = 56,
                IsReadOnly = true, CanUserSort = false};
            var checkedTemplate = new DataTemplate();
            var check = new FrameworkElementFactory(typeof(CheckBox));
            check.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            check.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            check.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Marked") {
                Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged});
            check.SetBinding(UIElement.IsEnabledProperty, new Binding("CanMark"));
            checkedTemplate.VisualTree = check;
            checkedColumn.CellTemplate = checkedTemplate;
            table.Columns.Add(checkedColumn);
            AddColumn(table, "Game in Playnite", "Title", 250);
            table.Columns.Add(StatusColumn());
            AddColumn(table, "Platform", "Platform", 155);
            AddColumn(table, "IGDB suggestion", "Candidate", 190);
            // Prioritize full platform and game names. Every cell has 12-DIP
            // side padding; truncated values retain accessible full-text tooltips.
            table.Columns[1].Width = new DataGridLength(1.65, DataGridLengthUnitType.Star);
            table.Columns[2].Width = new DataGridLength(0.95, DataGridLengthUnitType.Star);
            table.Columns[3].Width = new DataGridLength(1.30, DataGridLengthUnitType.Star);
            table.Columns[4].Width = new DataGridLength(1.15, DataGridLengthUnitType.Star);
            table.SelectionChanged += (sender, args) => {UpdateInspector(); UpdateSelectionSummary();};
            table.PreviewKeyDown += (sender, args) => {
                if (args.Key == Key.M && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {MarkHighlighted(true); args.Handled = true;}
                else if (args.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control &&
                    reviewFocusedButton != null && reviewFocusedButton.IsEnabled)
                {ReviewFocusedGame(); args.Handled = true;}
            };
            table.MouseDoubleClick += (sender, args) => {
                // Only a game row can open a dialog. In particular, double-clicking
                // Mark must not start a review or trigger the old details modal.
                var visual = args.OriginalSource as DependencyObject;
                DataGridCell clickedCell = null;
                while (visual != null && visual != table && !(visual is DataGridRow))
                {
                    if (visual is CheckBox) return;
                    if (clickedCell == null) clickedCell = visual as DataGridCell;
                    visual = VisualTreeHelper.GetParent(visual);
                }
                var hitRow = visual as DataGridRow;
                if (hitRow == null || (clickedCell != null && clickedCell.Column == checkedColumn)) return;
                var current = hitRow.Item as LibraryWorkspaceItem;
                if (current == null) return;
                // DataGrid normally selects on the first click, but bind the
                // target explicitly to prevent a stale focused GameId being used.
                if (table.SelectedItem != current) table.SelectedItem = current;
                if (reviewFocusedButton != null && reviewFocusedButton.IsEnabled)
                    ReviewFocusedGame();
                else ShowFocusedGameDetails();
                args.Handled = true;
            };
            table.PreviewMouseRightButtonDown += (sender,args) => {
                var visual=args.OriginalSource as DependencyObject;
                while(visual!=null && !(visual is DataGridRow))
                    visual=VisualTreeHelper.GetParent(visual);
                var target=visual as DataGridRow;
                table.SelectedItem=target==null ? null : target.Item;
            };
            var context = new ContextMenu();
            var markMenu = new MenuItem {Header = "Mark highlighted in preview"};
            markMenu.Click += (sender, args) => MarkHighlighted(true);
            context.Items.Add(markMenu);
            var unmarkMenu = new MenuItem {Header = "Unmark highlighted"};
            unmarkMenu.Click += (sender, args) => MarkHighlighted(false);
            context.Items.Add(unmarkMenu);
            var copyMenu = new MenuItem {Header = "Copy focused game title"};
            copyMenu.Click += (sender, args) => {
                var item = table.SelectedItem as LibraryWorkspaceItem;
                if (item != null) Clipboard.SetText(item.Title);
            };
            context.Items.Add(copyMenu);
            var copyIgdb = new MenuItem {Header="Copy saved IGDB candidate ID"};
            copyIgdb.Click += (sender,args) => {
                var item=table.SelectedItem as LibraryWorkspaceItem;
                long candidateId;
                if(item!=null && Int64.TryParse(item.CachedCatalogId,out candidateId) && candidateId>0)
                    Clipboard.SetText(item.CachedCatalogId);
            };
            context.Items.Add(copyIgdb);
            context.Items.Add(new Separator());
            var viewDetails = new MenuItem {Header="View game details (read-only)..."};
            viewDetails.Click += (sender,args) => ShowFocusedGameDetails();
            context.Items.Add(viewDetails);
            var reviewOne = new MenuItem {Header="Review this game only..."};
            reviewOne.Click += (sender,args) => ReviewFocusedGame();
            context.Items.Add(reviewOne);
            var checkFields = new MenuItem {Header="Inspect / compare IGDB fields (read-only)..."};
            checkFields.Click += (sender,args) => CheckLiveFieldAvailability();
            context.Items.Add(checkFields);
            var chooseIdentity = new MenuItem {Header="Find / correct IGDB identity..."};
            chooseIdentity.Click += (sender,args) => ShowIdentitySearch();
            context.Items.Add(chooseIdentity);
            var editProposal = new MenuItem {Header="Choose fields / custom text (draft only)..."};
            editProposal.Click += (sender,args) => EditFocusedDraft();
            context.Items.Add(editProposal);
            var ignoreMenu = new MenuItem {Header="Ignore / restore focused game..."};
            ignoreMenu.Click += (sender,args) => ToggleFocusedIgnore();
            context.Items.Add(ignoreMenu);
            var openHistory = new MenuItem {Header="Open import History..."};
            openHistory.Click += (sender,args) => ShowHistoryPreview();
            context.Items.Add(openHistory);
            context.Opened += (sender,args) => {
                var item = table.SelectedItem as LibraryWorkspaceItem;
                bool idle=!scanning && !checkingLiveFields && !checkingBatchFields && !nativeApplying;
                markMenu.IsEnabled=idle && item!=null && item.CanMark;
                unmarkMenu.IsEnabled=idle && item!=null;
                copyMenu.IsEnabled=item!=null;
                viewDetails.IsEnabled=item!=null;
                long id;
                copyIgdb.IsEnabled=item!=null && Int64.TryParse(item.CachedCatalogId,out id) && id>0;
                reviewOne.IsEnabled=idle && reviewFocusedButton!=null &&
                    reviewFocusedButton.IsEnabled;
                reviewOne.Header=NeedsManualIdentity(item) ?
                    "Find IGDB match..." : "Review this game only...";
                checkFields.IsEnabled=idle && checkLiveFieldsButton!=null && checkLiveFieldsButton.IsEnabled;
                chooseIdentity.IsEnabled=idle && identitySearchButton!=null && identitySearchButton.IsEnabled;
                editProposal.IsEnabled=idle && editDraftButton!=null && editDraftButton.IsEnabled;
                ignoreMenu.IsEnabled=idle && ignoreRestoreButton!=null && ignoreRestoreButton.IsEnabled;
                ignoreMenu.Header=item!=null && item.Group==WorkspaceTaskGroup.Ignored ?
                    "Restore focused game..." : "Ignore focused game...";
                openHistory.IsEnabled=idle;
            };
            table.ContextMenu = context;
            listLayout.Children.Add(table);

            var splitter = new GridSplitter {Width = 8, Background = Background,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ResizeDirection = GridResizeDirection.Columns,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                ShowsPreview = true};
            Grid.SetColumn(splitter, 1); columns.Children.Add(splitter);
            var inspector = Frame(Panel, Stroke, new Thickness(0));
            Grid.SetColumn(inspector, 2); columns.Children.Add(inspector);
            var inspectorLayout = new DockPanel(); inspector.Child = inspectorLayout;
            var inspectorHeading = new Grid {Margin = new Thickness(13, 8, 10, 7)};
            inspectorHeading.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
            inspectorHeading.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            DockPanel.SetDock(inspectorHeading, Dock.Top); inspectorLayout.Children.Add(inspectorHeading);
            var inspectorCaption = Label("GAME DETAILS", 12, Muted);
            inspectorCaption.FontWeight = FontWeights.SemiBold;
            inspectorHeading.Children.Add(inspectorCaption);
            var paging = new WrapPanel {HorizontalAlignment = HorizontalAlignment.Right};
            previousGameButton = ActionButton("Previous", "\uE76B", () => MoveFocused(-1));
            nextGameButton = ActionButton("Next", "\uE76C", () => MoveFocused(1));
            previousGameButton.Height = 32; nextGameButton.Height = 32;
            previousGameButton.MinHeight = 32; nextGameButton.MinHeight = 32;
            paging.Children.Add(previousGameButton); paging.Children.Add(nextGameButton);
            Grid.SetColumn(paging, 1); inspectorHeading.Children.Add(paging);
            var scroll = new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled};
            inspectorLayout.Children.Add(scroll);
            var detail = new StackPanel {Margin = new Thickness(13, 2, 13, 13)};
            scroll.Content = detail;
            var titleGrid = new Grid {Margin = new Thickness(0, 0, 0, 5)};
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(66)});
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
            var titleArt = Frame(PanelRaised, SoftStroke, new Thickness(3));
            titleArt.Width = 60; titleArt.Height = 79;
            var titleArtwork = new Grid();
            headlineArtFallback = Label("No cover", 10, Muted);
            headlineArtFallback.HorizontalAlignment = HorizontalAlignment.Center;
            headlineArtFallback.TextAlignment = TextAlignment.Center;
            titleArtwork.Children.Add(headlineArtFallback);
            headlineCover = new Image {Stretch = Stretch.Uniform};
            titleArtwork.Children.Add(headlineCover);
            titleArt.Child = titleArtwork;
            titleGrid.Children.Add(titleArt);
            var titleLine = new StackPanel {Margin = new Thickness(9, 0, 0, 0)};
            Grid.SetColumn(titleLine, 1);
            titleGrid.Children.Add(titleLine);
            detailTitle = Label("Choose a game", 20, Text);
            detailTitle.FontWeight = FontWeights.SemiBold;
            titleLine.Children.Add(detailTitle);
            detailPlatform = Label("", 12, Muted);
            titleLine.Children.Add(detailPlatform);
            detailStatus = Label("", 12, Accent);
            detailStatus.FontWeight = FontWeights.SemiBold;
            detailStatus.Margin = new Thickness(0, 4, 0, 7);
            titleLine.Children.Add(detailStatus);
            detail.Children.Add(titleGrid);
            detailMatch = Label("—", 14, Text);
            detail.Children.Add(DetailSection("\uE721", "SUGGESTED IGDB MATCH", Accent, detailMatch));
            detailVerification = Label("", 11, Warning);
            detailVerification.Margin = new Thickness(3, -5, 0, 9);
            detailVerification.Visibility = Visibility.Collapsed;
            detail.Children.Add(detailVerification);

            // Primary task card. The same review, field-check and draft handlers
            // are reused; this layout does not create a second import path.
            var nextCard = Frame(PanelRaised, Stroke, new Thickness(12, 10, 12, 9));
            nextCard.Margin = new Thickness(0, 0, 0, 11);
            var nextBody = new StackPanel();
            var nextCaption = Label("REVIEW & PREPARE", 12, Accent);
            nextCaption.FontWeight = FontWeights.SemiBold;
            nextCaption.Margin = new Thickness(0, 0, 0, 8);
            nextBody.Children.Add(nextCaption);
            reviewFocusedButton = ActionButton("Review this game...", "\uE8A7", ReviewFocusedGame, true);
            reviewFocusedButton.Margin = new Thickness(0, 0, 0, 8);
            reviewFocusedButton.ToolTip = "Review only this game, or find its IGDB match first if none was saved. " +
                "No metadata changes without live checks and final approval.";
            ToolTipService.SetShowOnDisabled(reviewFocusedButton, true);
            System.Windows.Automation.AutomationProperties.SetName(reviewFocusedButton, "Review focused game");
            nextBody.Children.Add(reviewFocusedButton);
            detailFieldSummary = Label("No game selected", 12, Muted);
            detailFieldSummary.Margin = new Thickness(0, 0, 0, 8);
            nextBody.Children.Add(detailFieldSummary);
            checkLiveFieldsButton = ActionButton("Check IGDB field availability", "\uE721",
                CheckLiveFieldAvailability);
            checkLiveFieldsButton.Margin = new Thickness(0, 0, 0, 7);
            checkLiveFieldsButton.ToolTip = "Fetch exact IGDB ID and compare available fields with missing Playnite fields. " +
                "Read-only; this is not an import approval or a JPEG download test.";
            nextBody.Children.Add(checkLiveFieldsButton);
            liveFieldStatus = Label("Saved suggestions require live field checking.", 11, Muted);
            liveFieldStatus.Margin = new Thickness(0, 0, 0, 8);
            nextBody.Children.Add(liveFieldStatus);
            editDraftButton = ActionButton("Choose fields / custom text", "\uE70F", EditFocusedDraft);
            editDraftButton.Margin = new Thickness(0, 0, 0, 7);
            editDraftButton.ToolTip = "Check exact IGDB fields first. Select which IGDB fields to include, " +
                "or type a custom display title or description. Other field values are not inline-editable. " +
                "The draft is session-only; import still needs verification and approval.";
            ToolTipService.SetShowOnDisabled(editDraftButton, true);
            nextBody.Children.Add(editDraftButton);
            draftStatus = Label("No draft saved.", 11, Muted);
            draftStatus.Margin = new Thickness(0, 0, 0, 4);
            nextBody.Children.Add(draftStatus);
            nextCard.Child = nextBody;
            detail.Children.Add(nextCard);

            // Less-frequent identity/game management is still discoverable but
            // no longer competes with the primary review and field-check actions.
            var manageBody = new StackPanel {Margin = new Thickness(9, 7, 5, 4)};
            refreshFocusedButton = ActionButton("Refresh this IGDB match", "\uE72C", () => StartSavedRefresh(false));
            refreshFocusedButton.ToolTip = "Fresh read-only IGDB lookup for this game. " +
                "Identity confirmation and final metadata approval are separate.";
            refreshFocusedButton.Margin = new Thickness(0, 0, 0, 8);
            manageBody.Children.Add(refreshFocusedButton);
            var identityActions = new WrapPanel {Margin = new Thickness(0, 0, 0, 8)};
            identitySearchButton = ActionButton("Find / correct IGDB match", "\uE721", ShowIdentitySearch);
            identitySearchButton.ToolTip = "Search IGDB or enter an exact ID; choosing an identity does not confirm or import it";
            identityActions.Children.Add(identitySearchButton);
            identityConfirmButton = ActionButton("Confirm saved identity", "\uE8FB", StartConfirmIdentity);
            identityConfirmButton.ToolTip = "Explicitly remember this game's exact IGDB ID and title after live verification; never import approval";
            identityActions.Children.Add(identityConfirmButton);
            identityForgetButton = ActionButton("Forget confirmation", "\uE711", ForgetFocusedIdentity);
            identityForgetButton.ToolTip = "Revoke an identity decision; Playnite metadata and history are unchanged";
            identityActions.Children.Add(identityForgetButton);
            manageBody.Children.Add(identityActions);
            ignoreRestoreButton = ActionButton("Ignore this game", "\uE711", ToggleFocusedIgnore);
            ignoreRestoreButton.ToolTip = "Exclude or restore this game from Smart Metadata Matcher review. " +
                "Saved IGDB matches and Playnite metadata stay unchanged.";
            ignoreRestoreButton.Margin = new Thickness(0, 0, 0, 9);
            manageBody.Children.Add(ignoreRestoreButton);
            var evidencePanel = new StackPanel {Margin = new Thickness(2, 0, 0, 7)};
            detailEvidence = Label("No saved IGDB lookup for this game.", 11, Muted);
            evidencePanel.Children.Add(detailEvidence);
            manageBody.Children.Add(new Expander {Header = "Saved match evidence · not verified",
                IsExpanded = false, Foreground = Muted, Background = Panel,
                Margin = new Thickness(0, 0, 0, 7), Content = evidencePanel});
            detail.Children.Add(new Expander {Header = "Manage IGDB match / ignore game",
                IsExpanded = false, Foreground = Text, Background = Panel,
                Margin = new Thickness(0, 0, 0, 10), Content = manageBody});
            var proposedTitle = Label("POSSIBLE FIELD CHANGES · READ-ONLY", 13, Text);
            proposedTitle.FontWeight = FontWeights.SemiBold;
            proposedTitle.Margin = new Thickness(0, 0, 0, 6);
            detail.Children.Add(proposedTitle);
            fieldComparison = new DataGrid {
                AutoGenerateColumns = false, IsReadOnly = true,
                CanUserAddRows = false, CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                IsTabStop = false, Focusable = false,
                Background = Panel, Foreground = Text, RowBackground = Panel,
                AlternatingRowBackground = AlternateRow, AlternationCount = 2,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = SoftStroke,
                ColumnHeaderHeight = 30, RowHeight = 34,
                BorderBrush = SoftStroke, BorderThickness = new Thickness(1),
                MaxHeight = 240
            };
            // The comparison is information, not a multi-select field editor.
            // Suppress row clicks only; do not block scrollbars, mouse wheel,
            // tooltips or navigation/marking in the main library grid.
            MouseButtonEventHandler preventPreviewSelection = (sender, args) => {
                var hit = args.OriginalSource as DependencyObject;
                while (hit != null && hit != fieldComparison && !(hit is DataGridRow))
                    hit = VisualTreeHelper.GetParent(hit);
                if (hit is DataGridRow) args.Handled = true;
            };
            fieldComparison.PreviewMouseLeftButtonDown += preventPreviewSelection;
            fieldComparison.PreviewMouseRightButtonDown += preventPreviewSelection;
            var previewCells = new Style(typeof(DataGridCell));
            previewCells.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            previewCells.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            previewCells.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            previewCells.Setters.Add(new Setter(UIElement.FocusableProperty, false));
            fieldComparison.CellStyle = previewCells;
            var previewRows = new Style(typeof(DataGridRow));
            previewRows.Setters.Add(new Setter(Control.BackgroundProperty, Panel));
            previewRows.Setters.Add(new Setter(Control.ForegroundProperty, Text));
            previewRows.Setters.Add(new Setter(UIElement.FocusableProperty, false));
            var previewAlternate = new Trigger {Property = ItemsControl.AlternationIndexProperty, Value = 1};
            previewAlternate.Setters.Add(new Setter(Control.BackgroundProperty, AlternateRow));
            previewRows.Triggers.Add(previewAlternate);
            fieldComparison.RowStyle = previewRows;
            AddColumn(fieldComparison, "Field", "Field", 105);
            AddColumn(fieldComparison, "In Playnite", "Current", 118);
            AddColumn(fieldComparison, "Possible action", "Proposed", 250);
            fieldComparison.Columns[0].Width = new DataGridLength(1.1, DataGridLengthUnitType.Star);
            fieldComparison.Columns[1].Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
            fieldComparison.Columns[2].Width = new DataGridLength(2.1, DataGridLengthUnitType.Star);
            detail.Children.Add(fieldComparison);
            draftStatus = Label("No draft saved.", 11, Muted);
            draftStatus.Margin = new Thickness(0, 0, 0, 8);
            detail.Children.Add(draftStatus);
            detailMissing = Label("—", 12, Text);
            detailMissing.Visibility = Visibility.Collapsed;
            detailHelp = Label("", 12, Muted);
            detail.Children.Add(DetailSection("\uE7BA", "WHAT HAPPENS NEXT", Warning, detailHelp));
            // Artwork is secondary to identity and field diffs, but is still
            // discoverable and never fetched from IGDB on row selection.
            var galleryCard = Frame(PanelRaised, SoftStroke, new Thickness(10));
            var galleryBody = new StackPanel();
            galleryBody.Children.Add(Label("EXISTING PLAYNITE ARTWORK", 12, Muted));
            var gallery = new WrapPanel {Margin = new Thickness(0, 8, 0, 7)};
            existingCover = new Image(); existingIcon = new Image(); existingBackground = new Image();
            gallery.Children.Add(ArtworkTile(existingCover, "Current cover", 85, 113));
            gallery.Children.Add(ArtworkTile(existingIcon, "Icon", 64, 72));
            gallery.Children.Add(ArtworkTile(existingBackground, "Background", 100, 68));
            galleryBody.Children.Add(gallery);
            artworkNote = Label("Playnite images are local. Suggested images are not loaded automatically.", 11, Muted);
            galleryBody.Children.Add(artworkNote);
            galleryBody.Children.Add(Label("SUGGESTED IGDB ARTWORK · UNVERIFIED", 11, Muted));
            var suggestedGallery = new WrapPanel {Margin = new Thickness(0, 7, 0, 7)};
            suggestedCover = new Image(); suggestedScreenshot = new Image();
            suggestedCover.ImageFailed += (sender, args) => {
                if (artworkNote != null) artworkNote.Text = "Suggested cover could not load; nothing was changed.";};
            suggestedScreenshot.ImageFailed += (sender, args) => {
                if (artworkNote != null) artworkNote.Text = "Suggested screenshot could not load; nothing was changed.";};
            suggestedGallery.Children.Add(ArtworkTile(suggestedCover, "Suggested cover", 92, 114));
            suggestedGallery.Children.Add(ArtworkTile(suggestedScreenshot, "Screenshot", 132, 78));
            galleryBody.Children.Add(suggestedGallery);
            suggestedArtButton = ActionButton("Preview suggested artwork (online)", "\uE91B", LoadSuggestedArtwork);
            suggestedArtButton.IsEnabled = false;
            suggestedArtButton.ToolTip = "Only loads images from images.igdb.com on your explicit request";
            galleryBody.Children.Add(suggestedArtButton);
            galleryCard.Child = galleryBody;
            artworkExpander = new Expander {Header = "Artwork · current and suggested",
                IsExpanded = false, Foreground = Text, Background = Panel,
                Margin = new Thickness(0, 2, 0, 10), Content = galleryCard};
            artworkExpander.Expanded += (sender, args) => UpdateInspector();
            detail.Children.Add(artworkExpander);
            var footer = Frame(Background, SoftStroke, new Thickness(10, 8, 10, 8));
            footer.Child = IconLabel("\uE72E", "No changes made", Muted, 11);
            detail.Children.Add(footer);

            scope.SelectionChanged += (sender, args) => {
                onlyIgnoredView = false; ApplyFilter(); UpdateToolbarActions(); UpdateNavigation();};
            search.TextChanged += (sender, args) => {ApplyFilter(); UpdateToolbarActions();};
            platformFilter.SelectionChanged += (sender, args) => {ApplyFilter(); UpdateToolbarActions();};
            missingFilter.SelectionChanged += (sender, args) => {ApplyFilter(); UpdateToolbarActions();};
            window.PreviewKeyDown += (sender, args) => {
                if (args.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {search.Focus(); search.SelectAll(); args.Handled = true;}
                else if (args.Key == Key.Escape && search.IsKeyboardFocusWithin && search.Text.Length > 0)
                {search.Clear(); args.Handled = true;}
                else if (args.Key == Key.F5) {SafeReload(); args.Handled = true;}
            };
            window.Content = root;
            scope.SelectedIndex = 0;
            if (returnState != null)
                foreach (var id in returnState.MarkedGameIds) markedIds.Add(id);
            SafeReload();
            RestoreReturnState();
            UpdateScanControls();
            window.ShowDialog();
        }
        private void RestoreReturnState()
        {
            if (returnState == null || table == null || table.ItemsSource == null) return;
            // ReadLocalLibrary has already discarded completed, ignored, and
            // unavailable marks. No cached suggestion becomes write eligible.
            scope.SelectedIndex = returnState.CategoryIndex;
            search.Text = returnState.SearchText;
            if (platformFilter.Items.Contains(returnState.Platform))
                platformFilter.SelectedItem = returnState.Platform;
            else platformFilter.SelectedIndex = 0;
            missingFilter.SelectedIndex = returnState.MissingFilterIndex;
            ApplyFilter();
            var view = CollectionViewSource.GetDefaultView(table.ItemsSource);
            var focused = items.FirstOrDefault(x => x.GameId == returnState.FocusedGameId);
            if (focused != null && view != null && view.Contains(focused))
            {
                table.SelectedItem = focused;
                table.ScrollIntoView(focused);
            }
            UpdateSelectionSummary();
            notice.Text = "Returned from selected review. The library was refreshed; " +
                "completed or ignored games are no longer marked. " +
                markedIds.Count + " remaining preview marks kept. " +
                "No further metadata updates occur without approval.";
        }

        private void SafeReload()
        {
            if (scanning || checkingLiveFields) return;
            liveFieldResult = null; // Revalidate data and identity on every library reload.
            try {ReadLocalLibrary();}
            catch (Exception ex)
            {
                // Fail visibly rather than turning an unreadable cache/history
                // into an inaccurate claim that everything needs rescanning.
                items.Clear();
                knownGames.Clear();
                markedIds.Clear();
                table.ItemsSource = null;
                totals.Text = "The library overview could not be loaded safely.";
                notice.Text = "Read-only data check failed: " + ex.Message +
                    " Use Advanced > Inspect recovery ledgers (read-only), then Classic if legacy " +
                    "repair is needed. No data was changed.";
                notice.Foreground = Warning;
                noticeFrame.BorderBrush = Warning;
                metricSuggestions.Text = "—";
                metricChoices.Text = "—";
                metricPending.Text = "—";
                metricUpdated.Text = "—";
                metricOther.Text = "—";
                metricAll.Text = "—";
                UpdateInspector();
            }
        }
    }

    internal sealed class WorkspaceFieldPreviewRow
    {
        public string Field {get; set;}
        public string Current {get; set;}
        public string Proposed {get; set;}
    }
    internal sealed class WorkspaceHistoryPreviewRow
    {
        public Guid OperationId {get; set;}
        public bool Hidden {get; set;}
        public string VisibilityLabel {get {return Hidden ? "Hidden" : "Shown";}}
        public string When {get; set;}
        public string Game {get; set;}
        public string Status {get; set;}
        public string Fields {get; set;}
    }
    internal sealed class LibraryWorkspaceItem : INotifyPropertyChanged
    {
        internal Guid GameId;
        internal WorkspaceTaskGroup Group;
        internal bool FromSaved;
        internal bool IdentityConfirmed;
        internal int MissingMask;
        internal string SuggestedCoverId, SuggestedScreenshotId, CachedFieldHints;
        internal string CachedCatalogId, CachedCatalogName, CachedMatchStatus,
            CachedEvidence, CachedScore, CachedScannedUtc;
        internal Action<LibraryWorkspaceItem> MarkedChanged;
        private bool isMarked;
        public event PropertyChangedEventHandler PropertyChanged;
        public bool CanMark {get {return Group == WorkspaceTaskGroup.Suggestions ||
            Group == WorkspaceTaskGroup.NeedsChoice || Group == WorkspaceTaskGroup.CheckNeeded ||
            Group == WorkspaceTaskGroup.NotFound || Group == WorkspaceTaskGroup.CouldNotCheck;}}
        public bool Marked
        {
            get {return isMarked;}
            set
            {
                bool requested = value && CanMark;
                if (isMarked == requested) return;
                isMarked = requested;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Marked"));
                if (MarkedChanged != null) MarkedChanged(this);
            }
        }
        internal string Detail;
        public string Title {get; set;}
        public string Platform {get; set;}
        public string Source {get; set;}
        public string Candidate {get; set;}
        public string Status {get; set;}
        public string NextAction {get; set;}
        public string Missing {get; set;}
        public string StatusGlyph {get {return LibraryWorkspacePreview.StatusGlyphFor(Group);}}
        public Brush StatusBrush {get {return LibraryWorkspacePreview.StatusBrushFor(Group);}}
    }
}
