using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Playnite.SDK.Models;
using Playnite.SDK;

namespace SmartMetadataMatcher
{
    internal sealed class PreviewRow : INotifyPropertyChanged
    {
        private bool marked;
        public event PropertyChangedEventHandler PropertyChanged;
        public Guid Id { get; set; }
        public string Original { get; set; }
        public string Proposed { get; set; }
        public string Platform { get; set; }
        public string Source { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string MissingFields { get; set; }
        public string MetadataStatus { get; set; }
        public bool BackgroundOnlyMissing { get; set; }
        public bool CanMark { get; set; }
        public bool HasCollision { get; set; }
        public bool Marked
        {
            get { return marked; }
            set
            {
                if (marked == value || (value && !CanMark)) return;
                marked = value;
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs("Marked"));
            }
        }
    }

    internal sealed class PreviewWindow
    {
        private readonly IPlayniteAPI api;
        private readonly RenameJournal journal;
        private Window window;
        private DataGrid grid;
        private TextBlock summary;
        private TextBox search;
        private ComboBox statusFilter;
        private ComboBox platformFilter;
        private ComboBox metadataFilter;
        private List<PreviewRow> rows = new List<PreviewRow>();
        private List<PreviewRow> visible = new List<PreviewRow>();
        private bool markingBatch;
        private bool updatingLibrary;

        internal PreviewWindow(IPlayniteAPI api, string privateDataPath)
        {
            this.api = api;
            journal = new RenameJournal(privateDataPath);
        }

        internal void Show()
        {
            window = api.Dialogs.CreateWindow(new WindowCreationOptions { ShowMinimizeButton = false });
            window.Title = "Smart Metadata Matcher " + typeof(PreviewWindow).Assembly.GetName().Version.ToString(3) + " — review and rename Playnite titles";
            window.Width = 1270;
            window.Height = 760;
            window.MinWidth = 780;
            window.MinHeight = 480;
            var owner = api.Dialogs.GetCurrentAppWindow();
            if (owner != null && owner != window) window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var root = new DockPanel { Margin = new Thickness(12) };
            var header = new StackPanel();
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            header.Children.Add(new TextBlock
            {
                Text = "Review titles and missing metadata",
                FontSize = 19,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            header.Children.Add(new TextBlock
            {
                Text = "Preview title proposals and missing cover/background/description. " +
                       "Review entries require manual approval; potential same-platform title collisions are blocked. " +
                       "Only explicitly confirmed renames change Playnite game names, never ROM files or artwork.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var filterBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            header.Children.Add(filterBar);
            filterBar.Children.Add(new TextBlock { Text = "Search", VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0) });
            search = new TextBox { MinWidth = 245, Width = 300, Margin = new Thickness(0, 0, 12, 0) };
            search.TextChanged += (s, e) => Refilter();
            filterBar.Children.Add(search);
            filterBar.Children.Add(new TextBlock { Text = "Status", VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0) });
            statusFilter = new ComboBox { Width = 115, Margin = new Thickness(0, 0, 12, 0) };
            foreach (var x in new[] { "All", "Ready", "Review", "Metadata only" }) statusFilter.Items.Add(x);
            statusFilter.SelectedIndex = 0;
            statusFilter.SelectionChanged += (s, e) => Refilter();
            filterBar.Children.Add(statusFilter);
            filterBar.Children.Add(new TextBlock { Text = "Platform", VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0) });
            platformFilter = new ComboBox { Width = 190, Margin = new Thickness(0, 0, 6, 0) };
            platformFilter.SelectedIndex = -1;
            platformFilter.SelectionChanged += (s, e) => Refilter();
            filterBar.Children.Add(platformFilter);
            filterBar.Children.Add(new TextBlock { Text = "Core metadata", VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 5, 0) });
            metadataFilter = new ComboBox { Width = 165, Margin = new Thickness(0, 0, 5, 0) };
            foreach (var x in new[] { "All", "Missing all", "Missing some", "Background only", "Complete" })
                metadataFilter.Items.Add(x);
            metadataFilter.SelectedIndex = 0;
            metadataFilter.SelectionChanged += (s, e) => Refilter();
            filterBar.Children.Add(metadataFilter);

            summary = new TextBlock { Margin = new Thickness(0, 0, 0, 7), TextWrapping = TextWrapping.Wrap };
            header.Children.Add(summary);
            grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Extended,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                FrozenColumnCount = 2
            };
            var check = new FrameworkElementFactory(typeof(CheckBox));
            check.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Marked")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            check.SetBinding(UIElement.IsEnabledProperty, new Binding("CanMark"));
            check.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Mark", Width = 55,
                CellTemplate = new DataTemplate { VisualTree = check }
            });
            AddText("Current name", "Original", 250);
            AddText("Suggested name", "Proposed", 260);
            AddTextWithTooltip("Platform", "Platform", 200);
            AddText("Source", "Source", 125);
            AddText("Status", "Status", 105);
            AddText("Core metadata", "MetadataStatus", 145);
            AddText("Missing fields", "MissingFields", 170);
            AddText("Why", "Reason", 390);

            var footer = new StackPanel();
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);
            var buttons = new WrapPanel { Margin = new Thickness(0, 10, 0, 5) };
            footer.Children.Add(buttons);
            Button(buttons, "Mark highlighted", () => SetMarkedSelected(true));
            Button(buttons, "Unmark highlighted", () => SetMarkedSelected(false));
            Button(buttons, "Check visible ready", () => SetMarkedVisible(true, true));
            Button(buttons, "Clear visible", () => SetMarkedVisible(false, false));
            Button(buttons, "Copy marked plan", CopyPlan);
            Button(buttons, "Rename marked...", ApplyRenames);
            Button(buttons, "Undo last batch...", UndoLastBatch);
            Button(buttons, "Refresh", Refresh);
            Button(buttons, "Close", () => window.Close());
            footer.Children.Add(new TextBlock
            {
                Text = "Only approved Playnite Name fields change. Private write-ahead undo journal; " +
                       "no ROM/file renames, artwork downloads, network activity, or startup scans.",
                TextWrapping = TextWrapping.Wrap
            });
            // Code-built TextBlocks are black by default even in dark Playnite themes.
            foreach (var tb in header.Children.OfType<TextBlock>())
                tb.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            foreach (var tb in filterBar.Children.OfType<TextBlock>())
                tb.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            foreach (var tb in footer.Children.OfType<TextBlock>())
                tb.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            root.Children.Add(grid);
            window.Content = root;
            Refresh();
            window.ShowDialog();
        }

        private static void Button(Panel p, string name, Action action)
        {
            var b = new Button { Content = name, Padding = new Thickness(9, 5, 9, 5),
                Margin = new Thickness(0, 0, 8, 4), MinWidth = 95 };
            b.Click += (s, e) => action();
            p.Children.Add(b);
        }

        private void AddText(string title, string property, double width)
        {
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = title, Binding = new Binding(property), Width = width, IsReadOnly = true
            });
        }

        private void AddTextWithTooltip(string title, string property, double width)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetBinding(TextBlock.TextProperty, new Binding(property));
            factory.SetBinding(FrameworkElement.ToolTipProperty, new Binding(property));
            factory.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = title, Width = width,
                CellTemplate = new DataTemplate { VisualTree = factory }, IsReadOnly = true
            });
        }

        private void Refresh()
        {
            var checkedBefore = new HashSet<string>(rows.Where(r => r.Marked)
                .Select(r => r.Id.ToString("D") + "\u001f" + r.Proposed));
            var games = api.Database.Games.Where(g => g != null && !String.IsNullOrWhiteSpace(g.Name))
                .ToList();
            var descriptors = games.Select(g => new GameDescriptor
            {
                Id = g.Id,
                Name = g.Name,
                Platform = g.Platforms == null ? "" : String.Join(", ", g.Platforms.Select(p => p.Name)),
                Source = g.Source == null ? "" : g.Source.Name
            }).ToList();
            var descriptorById = descriptors.ToDictionary(d => d.Id);
            var proposals = NameRules.Analyze(descriptors).ToDictionary(p => p.Id);
            rows = new List<PreviewRow>();
            foreach (var game in games)
            {
                RenameProposal proposal;
                proposals.TryGetValue(game.Id, out proposal);
                var metadata = new DiagnosticRecord
                {
                    HasCover = !String.IsNullOrWhiteSpace(game.CoverImage),
                    HasBackground = !String.IsNullOrWhiteSpace(game.BackgroundImage),
                    HasDescription = !String.IsNullOrWhiteSpace(game.Description)
                };
                if (proposal == null && !metadata.NeedsCoreMetadata) continue;
                var descriptor = descriptorById[game.Id];
                var canMark = proposal != null;
                var collision = canMark && proposal.Reason.IndexOf("title collision",
                    StringComparison.OrdinalIgnoreCase) >= 0;
                rows.Add(new PreviewRow
                {
                    Id = game.Id,
                    Original = game.Name,
                    Proposed = canMark ? proposal.Proposed : "(no title change)",
                    Platform = descriptor.Platform,
                    Source = descriptor.Source,
                    Status = canMark ? proposal.Status : "Metadata only",
                    Reason = canMark ? proposal.Reason : "No name rule matched; metadata needs attention",
                    MetadataStatus = metadata.CoreStatus,
                    MissingFields = metadata.MissingCoreFields,
                    BackgroundOnlyMissing = metadata.BackgroundOnlyMissing,
                    CanMark = canMark && !collision,
                    HasCollision = collision,
                    Marked = canMark && !collision && checkedBefore.Contains(game.Id.ToString("D") + "\u001f" + proposal.Proposed)
                });
            }
            rows = rows.OrderBy(r => r.Original, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var row in rows)
                row.PropertyChanged += (s, e) => { if (!markingBatch) UpdateSummary(); };
            string currentPlatform = platformFilter.SelectedItem as string;
            // Prevent nested rebuilds by temporarily ignoring the control's event through a flag.
            rebuildingPlatforms = true;
            try
            {
                platformFilter.Items.Clear();
                platformFilter.Items.Add("All platforms");
                foreach (var p in rows.Select(r => String.IsNullOrWhiteSpace(r.Platform) ? "(Unspecified)" : r.Platform)
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    platformFilter.Items.Add(p);
                platformFilter.SelectedItem = currentPlatform != null && platformFilter.Items.Contains(currentPlatform)
                    ? currentPlatform : "All platforms";
            }
            finally { rebuildingPlatforms = false; }
            Refilter();
        }

        private bool rebuildingPlatforms;
        private void PlatformChanged(object sender, SelectionChangedEventArgs args) { Refilter(); }

        private void Refilter()
        {
            if (grid == null || statusFilter == null || platformFilter == null || metadataFilter == null || rebuildingPlatforms) return;
            var query = search.Text.Trim();
            var desiredStatus = statusFilter.SelectedItem as string ?? "All";
            var desiredPlatform = platformFilter.SelectedItem as string ?? "All platforms";
            var desiredMetadata = metadataFilter.SelectedItem as string ?? "All";
            visible = rows.Where(r =>
                (desiredStatus == "All" || r.Status == desiredStatus) &&
                (desiredMetadata == "All" ||
                 (desiredMetadata == "Missing all" && r.MetadataStatus == "Missing all core fields") ||
                 (desiredMetadata == "Missing some" && r.MetadataStatus == "Partially missing core fields") ||
                 (desiredMetadata == "Background only" && r.BackgroundOnlyMissing) ||
                 (desiredMetadata == "Complete" && r.MetadataStatus == "All core fields present")) &&
                (desiredPlatform == "All platforms" ||
                 String.Equals(String.IsNullOrWhiteSpace(r.Platform) ? "(Unspecified)" : r.Platform,
                     desiredPlatform, StringComparison.OrdinalIgnoreCase)) &&
                (query.Length == 0 || (r.Original + " " + r.Proposed + " " + r.Platform + " " + r.Source + " " + r.MissingFields)
                    .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            grid.ItemsSource = visible;
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (summary == null) return;
            summary.Text = rows.Count + " attention item(s); " +
                rows.Count(r => r.Status == "Ready") + " ready name(s); " +
                rows.Count(r => r.Status == "Review") + " review name(s); " +
                rows.Count(r => r.Status == "Metadata only") + " metadata-only; " +
                rows.Count(r => r.MetadataStatus == "Missing all core fields") + " missing all core fields; " +
                visible.Count + " visible; " + rows.Count(r => r.Marked) + " marked for approval.";
        }

        private void SetMarkedSelected(bool mark)
        {
            markingBatch = true;
            try { foreach (var row in grid.SelectedItems.Cast<object>().OfType<PreviewRow>().Where(r => r.CanMark)) row.Marked = mark; }
            finally { markingBatch = false; }
            UpdateSummary();
        }

        private void SetMarkedVisible(bool mark, bool onlyReady)
        {
            markingBatch = true;
            try { foreach (var row in visible.Where(r => r.CanMark && (!onlyReady || r.Status == "Ready"))) row.Marked = mark; }
            finally { markingBatch = false; }
            UpdateSummary();
        }

        private static TextBlock ThemeLine(string text, bool wrap)
        {
            var block = new TextBlock { Text = text, TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            return block;
        }

        // Scrollable full preview: no hidden changes beyond the first 10 rows.
        private bool ConfirmBatch(string title, string note, IList<string> descriptions, string verb)
        {
            bool accepted = false;
            var dialog = api.Dialogs.CreateWindow(new WindowCreationOptions { ShowMinimizeButton = false });
            dialog.Title = title;
            dialog.Width = 820;
            dialog.Height = 610;
            dialog.MinWidth = 500;
            dialog.MinHeight = 350;
            if (window != null) dialog.Owner = window;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var root = new DockPanel { Margin = new Thickness(12) };
            var top = new StackPanel();
            DockPanel.SetDock(top, Dock.Top);
            root.Children.Add(top);
            var heading = ThemeLine(note, true);
            heading.Margin = new Thickness(0, 0, 0, 10);
            top.Children.Add(heading);
            var footer = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0) };
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);
            Button(footer, "Cancel", () => dialog.Close());
            Button(footer, verb, () => { accepted = true; dialog.Close(); });
            var list = new ListBox { ItemsSource = descriptions, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            root.Children.Add(list);
            dialog.Content = root;
            dialog.ShowDialog();
            return accepted;
        }

        private List<GameDescriptor> GetDescriptors(IEnumerable<Game> games)
        {
            return games.Where(g => g != null && !String.IsNullOrWhiteSpace(g.Name))
                .Select(g => new GameDescriptor
                {
                    Id = g.Id, Name = g.Name,
                    Platform = g.Platforms == null ? "" : String.Join(", ", g.Platforms.Select(p => p.Name)),
                    Source = g.Source == null ? "" : g.Source.Name
                }).ToList();
        }

        private void ApplyRenames()
        {
            if (updatingLibrary) return;
            var selected = rows.Where(r => r.Marked && r.CanMark).ToList();
            if (selected.Count == 0)
            {
                api.Dialogs.ShowMessage("Mark eligible title proposals first. Collisions cannot be renamed automatically.",
                    "Smart Metadata Matcher");
                return;
            }
            // Revalidate every proposed action from a fresh library snapshot BEFORE confirmation.
            var games = api.Database.Games.Where(g => g != null).ToList();
            var byId = games.ToDictionary(g => g.Id);
            var liveProposals = NameRules.Analyze(GetDescriptors(games)).ToDictionary(p => p.Id);
            var validated = new List<PreviewRow>();
            var blocked = new List<string>();
            foreach (var row in selected)
            {
                Game game;
                RenameProposal proposed;
                if (!byId.TryGetValue(row.Id, out game) || game == null ||
                    !String.Equals(game.Name, row.Original, StringComparison.Ordinal) ||
                    !liveProposals.TryGetValue(row.Id, out proposed) ||
                    !String.Equals(proposed.Proposed, row.Proposed, StringComparison.Ordinal) ||
                    proposed.Status != row.Status ||
                    proposed.Reason.IndexOf("title collision", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    blocked.Add(row.Original + " (stale, changed, or colliding)");
                    continue;
                }
                validated.Add(row);
            }
            if (validated.Count == 0)
            {
                api.Dialogs.ShowMessage("No marked changes remain eligible. Refresh the review and check collisions.",
                    "Smart Metadata Matcher");
                Refresh();
                return;
            }
            var preview = validated.Select(r => r.Original + "  →  " + r.Proposed +
                "   [" + r.Platform + "]" + (r.Status == "Review" ? "  MANUAL REVIEW" : "")).ToList();
            string note = "Rename " + validated.Count + " Playnite library title(s)? " + blocked.Count +
                " marked item(s) will be skipped due to changed data or collisions. " +
                "Review entries require individual identity verification. Only Name changes; ROMs, play actions " +
                "and existing metadata remain intact. Each attempted change is journaled before it is applied.";
            if (!ConfirmBatch("Confirm Playnite title renames", note, preview, "Rename in Playnite")) return;
            var batch = Guid.NewGuid();
            int updated = 0;
            int stale = blocked.Count;
            string failure = null;
            updatingLibrary = true;
            try
            {
                foreach (var row in validated)
                {
                    var live = api.Database.Games[row.Id];
                    if (live == null || !String.Equals(live.Name, row.Original, StringComparison.Ordinal))
                    {
                        stale++;
                        continue;
                    }
                    // Do not rerun collection-sequence heuristics after every rename:
                    // removing 01 changes the detected sequence for 02/03 mid-batch.
                    // Preflight already checked proposals. Recheck only live target collisions.
                    bool newCollision = GetDescriptors(api.Database.Games).Any(d =>
                        d.Id != row.Id && String.Equals(d.Name, row.Proposed, StringComparison.OrdinalIgnoreCase) &&
                        String.Equals(d.Platform, row.Platform, StringComparison.OrdinalIgnoreCase) &&
                        String.Equals(d.Source, row.Source, StringComparison.OrdinalIgnoreCase));
                    if (newCollision)
                    {
                        stale++;
                        continue;
                    }
                    journal.Prepare(batch, row.Id, row.Original, row.Proposed);
                    live.Name = row.Proposed;
                    api.Database.Games.Update(live);
                    updated++;
                }
            }
            catch (Exception ex) { failure = ex.Message; }
            finally { updatingLibrary = false; Refresh(); }
            var message = "Changed " + updated + " Playnite title(s). Skipped " + stale + ". " +
                "Undo last batch is available for exact-title matches. " +
                "No ROM files or launch actions were changed.";
            if (failure != null)
                api.Dialogs.ShowErrorMessage(message + "\nOperation stopped: " + failure,
                    "Smart Metadata Matcher");
            else api.Dialogs.ShowMessage(message, "Smart Metadata Matcher");
        }

        private void UndoLastBatch()
        {
            if (updatingLibrary) return;
            List<JournalEntry> latest;
            try { latest = journal.LatestPendingBatch(); }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Cannot read the private undo journal; no changes attempted: " +
                    ex.Message, "Smart Metadata Matcher");
                return;
            }
            if (latest.Count == 0)
            {
                api.Dialogs.ShowMessage("No pending rename batch was found in the private journal.",
                    "Smart Metadata Matcher");
                return;
            }
            var valid = new List<JournalEntry>();
            var notApplied = new List<JournalEntry>();
            var unavailable = new List<string>();
            foreach (var entry in latest)
            {
                var game = api.Database.Games[entry.GameId];
                if (game != null && String.Equals(game.Name, entry.After, StringComparison.Ordinal))
                    valid.Add(entry);
                else if (game != null && String.Equals(game.Name, entry.Before, StringComparison.Ordinal))
                    notApplied.Add(entry); // interrupted write-ahead record; no DB change to reverse
                else unavailable.Add(entry.After);
            }
            if (valid.Count == 0 && notApplied.Count == 0)
            {
                api.Dialogs.ShowMessage("The latest journaled batch has no unchanged rename results to undo. " +
                    "If titles were edited afterward, no automatic overwrite is permitted.",
                    "Smart Metadata Matcher");
                return;
            }
            var plan = valid.Select(e => e.After + "  →  " + e.Before).ToList();
            plan.AddRange(notApplied.Select(e => e.Before + " (already original; clear journal marker only)"));
            if (!ConfirmBatch("Confirm undo of latest rename batch",
                "Restore " + valid.Count + " Playnite title(s) and clear " + notApplied.Count +
                " unapplied journal records? " + unavailable.Count +
                " unavailable/stale entries will remain untouched. Undo changes only names that still " +
                "exactly match this plugin's recorded result.", plan, "Restore titles")) return;
            int restored = 0;
            int skipped = unavailable.Count;
            string failure = null;
            updatingLibrary = true;
            try
            {
                foreach (var entry in notApplied)
                    journal.MarkUndone(entry.BatchId, entry.GameId);
                foreach (var entry in valid)
                {
                    var game = api.Database.Games[entry.GameId];
                    if (game == null || !String.Equals(game.Name, entry.After, StringComparison.Ordinal))
                    {
                        skipped++;
                        continue;
                    }
                    game.Name = entry.Before;
                    api.Database.Games.Update(game);
                    journal.MarkUndone(entry.BatchId, entry.GameId);
                    restored++;
                }
            }
            catch (Exception ex) { failure = ex.Message; }
            finally { updatingLibrary = false; Refresh(); }
            var message = "Restored " + restored + " name(s). Skipped " + skipped +
                " changed/unavailable entries. Game files and metadata were untouched.";
            if (failure == null) api.Dialogs.ShowMessage(message, "Smart Metadata Matcher");
            else api.Dialogs.ShowErrorMessage(message + "\nUndo stopped: " + failure,
                "Smart Metadata Matcher");
        }

        private void CopyPlan()
        {
            var marked = rows.Where(r => r.Marked && r.CanMark).ToList();
            if (marked.Count == 0)
            {
                api.Dialogs.ShowMessage("No rows marked. Mark one or more rows to copy proposals.", "Smart Metadata Matcher");
                return;
            }
            var text = new StringBuilder();
            text.AppendLine("Original name\tSuggested name\tPlatform\tStatus\tReason");
            foreach (var row in marked)
                text.AppendLine(Flat(row.Original) + "\t" + Flat(row.Proposed) + "\t" + Flat(row.Platform)
                    + "\t" + Flat(row.Status) + "\t" + Flat(row.Reason));
            try
            {
                Clipboard.SetText(text.ToString());
                api.Dialogs.ShowMessage(marked.Count + " proposed rename(s) copied to clipboard. No Playnite data changed.",
                    "Smart Metadata Matcher");
            }
            catch (Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Could not copy the review plan: " + ex.Message,
                    "Smart Metadata Matcher");
            }
        }
        private static string Flat(string s)
        {
            return (s ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
