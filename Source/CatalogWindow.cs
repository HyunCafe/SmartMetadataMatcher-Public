using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    internal sealed class CatalogViewRow
    {
        public Guid GameId { get; set; }
        public string CurrentName { get; set; }
        public string Platform { get; set; }
        public string Candidate { get; set; }
        public string Status { get; set; }
        public string Evidence { get; set; }
        public string IgdbId { get; set; }
        public string AvailableMetadata { get; set; }
        public string MatchScore { get; set; }
        public string Queries { get; set; }
        public string Alternatives { get; set; }
        public string ReleaseYear { get; set; }
        public string FoundCount { get; set; }
        public bool IsCompleted { get; set; }
        public string PreviousStatus { get; set; }
        public bool FromHistory { get; set; }
        public bool IsIgnored { get; set; }
        public string CatalogSummary { get; set; }
        public string CatalogCoverImageId { get; set; }
        public string CatalogScreenshotImageId { get; set; }
        // Checkbox state and highlighted rows are intentionally independent.
        public bool Marked { get; set; }
        // Set only from a fresh full resolver result; never inferred from score text.
        public bool AutoPlanEligible { get; set; }
    }

    internal sealed class PreviewFieldRow
    {
        public string Field { get; set; }
        public string Current { get; set; }
        public string Suggested { get; set; }
        public string Decision { get; set; }
    }

    // Preview data only: represents a candidate decision, never a write plan.
    internal sealed class PlanPreviewRow
    {
        internal CatalogViewRow SourceRow;
        public string Game { get; set; }
        public string Platform { get; set; }
        public string Catalog { get; set; }
        public string IdentityStatus { get; set; }
        public string Explanation { get; set; }
        public string RenameHandling { get; set; }
        public string CatalogId { get; set; }
        public bool Ready { get; set; }
        public bool Archived { get; set; }
        // The review dialog and dashboard share ONE selection state.
        public bool Marked
        {
            get { return SourceRow!=null && SourceRow.Marked; }
            set { if(SourceRow!=null)SourceRow.Marked=value; }
        }
    }

    internal sealed class HistoryRow
    {
        public Guid OperationId { get; set; }
        public Guid GameId { get; set; }
        public string Game { get; set; }
        public string Fields { get; set; }
        public string When { get; set; }
        public string Result { get; set; }
        public bool Hidden { get; set; }
        public string VisibilityLabel { get { return Hidden?"Hidden":"Shown"; } }
    }

    // Early read-only integration checkpoint. Bulk application is intentionally NOT enabled
    // until candidate accuracy, rate limits, catalog terms and recovery are live-validated.
    internal sealed class CatalogWindow
    {
        private readonly IPlayniteAPI api;
        private readonly string pluginDataPath;
        private readonly Dictionary<long,CatalogGame> inspectedCatalog = new Dictionary<long,CatalogGame>();
        private Button inspectFieldsButton;
        private bool inspectingFields;
        private readonly CatalogCredentialStore credentialStore;
        private readonly MetadataJournal metadataJournal;
        private readonly EnrichmentJournal enrichmentJournal;
        private readonly CatalogIgnored ignoredStore;
        private readonly HistoryVisibilityStore historyVisibility;
        private HashSet<Guid> hiddenOperations = new HashSet<Guid>();
        private HashSet<Guid> ignoredIds = new HashSet<Guid>();
        private readonly List<Button> tabButtons = new List<Button>();
        private TextBox catalogSearch;
        private Button selectionToggleButton, visibleToggleButton, reviewSelectionButton;
        private Button clearImportSelectionButton, ignoreHighlightedButton;
        private Button restoreHistoryButton, removeRecentButton;
        private ComboBox platformFilter;
        private DataGrid historyGrid;
        private RowDefinition historyRow;
        private GridSplitter historySplitter;
        private TextBlock emptyHistory;
        private TextBlock previewTitle, previewDetails, previewSummary;
        private TextBlock previewChangeHeading, previewDiagnostics;
        private StackPanel previewHighlights;
        private Image previewCover, previewIcon, previewBackground;
        private DataGrid previewComparison;
        private readonly List<PreviewFieldRow> comparisonRows = new List<PreviewFieldRow>();
        private TextBlock previewState;

        private Window window;
        private TextBox clientId;
        private PasswordBox secret;
        private Button scanButton;
        private Button applyButton;
        private Button batchButton;
        private Button autoBatchButton;
        private Button undoButton;
        private TextBlock credentialStatus;
        private bool diagnosticColumnsVisible;
        private ComboBox scanSize;
        private ComboBox resultView;
        private CheckBox useCatalogTitle;
        private Button cancelButton;
        private TextBlock summary;
        private DataGrid grid;
        private List<CatalogInput> queue = new List<CatalogInput>();
        private readonly List<CatalogViewRow> results = new List<CatalogViewRow>();
        private CancellationTokenSource cancellation;
        private int cursor;
        private int excludedTesting;
        private bool busy;

        internal CatalogWindow(IPlayniteAPI apiValue, string pluginDataPath)
        {
            api = apiValue;
            this.pluginDataPath = pluginDataPath;
            credentialStore = new CatalogCredentialStore(pluginDataPath);
            metadataJournal = new MetadataJournal(pluginDataPath);
            enrichmentJournal = new EnrichmentJournal(pluginDataPath);
            ignoredStore = new CatalogIgnored(pluginDataPath);
            historyVisibility = new HistoryVisibilityStore(pluginDataPath);
            // Fail closed for malformed privacy/preferences state. Never silently
            // reset the user's hidden-record choices or mutate the undo journal.
            hiddenOperations = historyVisibility.Load();
        }
        private static SolidColorBrush UiColor(byte r,byte g,byte b)
        {
            var brush=new SolidColorBrush(Color.FromRgb(r,g,b));brush.Freeze();return brush;
        }
        private static Brush Accent() { return UiColor(50,193,240); }
        private static Brush Good() { return UiColor(102,225,148); }
        private static Brush Caution() { return UiColor(231,182,105); }
        private static Brush Attention() { return UiColor(244,154,140); }
        private static Brush Quiet() { return UiColor(150,174,203); }
        private static Brush Muted() { return UiColor(170,186,207); }
        private static Brush Deep() { return UiColor(10,18,37); }
        private static Brush PanelBrush() { return UiColor(15,27,51); }
        private static Brush StrokeBrush() { return UiColor(55,103,145); }
        private static void TextColor(TextBlock text)
        {
            text.SetResourceReference(TextBlock.ForegroundProperty,"TextBrush");
        }
        private static TextBlock Icon(string glyph,int size=15,Brush color=null)
        {
            var t=new TextBlock { Text=glyph,FontFamily=new FontFamily("Segoe MDL2 Assets"),
                FontSize=size,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,7,0) };
            t.Foreground=color??Accent();return t;
        }
        private static void Decorate(Button button,string glyph,bool primary=false)
        {
            var content=Convert.ToString(button.Content);
            var panel=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center };
            panel.Children.Add(Icon(glyph,14,primary?Deep():Accent()));
            var label=new TextBlock { Text=content,VerticalAlignment=VerticalAlignment.Center };
            if(primary)label.Foreground=Deep();else TextColor(label);
            panel.Children.Add(label);
            button.Content=panel;
            if(primary){button.Background=Accent();button.BorderBrush=Accent();button.FontWeight=FontWeights.SemiBold;}
        }
        private static void AddButton(Panel parent,string label,Action clicked,out Button button)
        {
            button=new Button {Content=label,Margin=new Thickness(0,0,7,5),
                Padding=new Thickness(12,7,12,7),MinHeight=36,FontSize=13,
                Background=PanelBrush(),BorderBrush=StrokeBrush(),BorderThickness=new Thickness(1)};
            button.SetResourceReference(Control.ForegroundProperty,"TextBrush");
            button.Click+=(s,e)=>clicked();parent.Children.Add(button);
        }
        private static Border Frame(UIElement content,Thickness padding)
        {
            return new Border{BorderBrush=StrokeBrush(),BorderThickness=new Thickness(1),
                CornerRadius=new CornerRadius(4),Background=PanelBrush(),Padding=padding,Child=content};
        }
        private TextBlock Caption(string value,int size=12)
        {
            var t=new TextBlock {Text=value,TextWrapping=TextWrapping.Wrap,FontSize=size,
                VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,2,0,5)};
            TextColor(t);return t;
        }
        private static void SetTabContent(Button button,string label,int index,bool selected)
        {
            var symbols=new[]{"\uE80F","\uE73E","\uE7BA","\uE711","\uE8A7"};
            var panel=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
            panel.Children.Add(Icon(symbols[index],17,
                index==2 && !label.EndsWith("(0)",StringComparison.Ordinal)?Caution():
                selected?Accent():Muted()));
            var text=new TextBlock{Text=label,FontSize=14,VerticalAlignment=VerticalAlignment.Center};
            text.Foreground=selected?Accent():Muted();panel.Children.Add(text);
            button.Content=panel;button.Background=selected?UiColor(15,42,70):PanelBrush();
            button.BorderBrush=selected?Accent():StrokeBrush();
            button.BorderThickness=new Thickness(0,0,0,selected?3:1);
            button.FontWeight=selected?FontWeights.SemiBold:FontWeights.Normal;
        }
        private Button MakeTab(string label,int index,Panel parent)
        {
            Button button;AddButton(parent,label,()=>{resultView.SelectedIndex=index;},out button);
            button.MinWidth=index==4?150:171;button.Padding=new Thickness(13,12,13,12);
            button.Margin=new Thickness(0,0,5,0);
            tabButtons.Add(button);SetTabContent(button,label,index,index==0);return button;
        }
        private static Style ResultStyle()
        {
            var style=new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty,Good()));
            foreach(var pair in new[]{
                Tuple.Create("Review",Caution()),Tuple.Create("Unmatched",Attention()),
                Tuple.Create("Ignored",Quiet()),Tuple.Create("Undone",Quiet())})
            {
                var trigger=new DataTrigger{Binding=new Binding("Result"),Value=pair.Item1};
                trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty,pair.Item2));
                style.Triggers.Add(trigger);
            }
            var selected=new DataTrigger{Binding=new Binding("IsSelected"){
                RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(DataGridRow),1)},Value=true};
            selected.Setters.Add(new Setter(TextBlock.ForegroundProperty,Deep()));
            style.Triggers.Add(selected);
            return style;
        }
        private static Style CandidateStatusStyle()
        {
            var style=new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.ForegroundProperty,Good()));
            foreach(var pair in new[]{
                Tuple.Create("Review",Caution()),Tuple.Create("Unmatched",Attention()),
                Tuple.Create("Ignored",Quiet())})
            {
                var trigger=new DataTrigger{Binding=new Binding("Status"),Value=pair.Item1};
                trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty,pair.Item2));
                style.Triggers.Add(trigger);
            }
            var selected=new DataTrigger{Binding=new Binding("IsSelected"){
                RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(DataGridRow),1)},Value=true};
            selected.Setters.Add(new Setter(TextBlock.ForegroundProperty,Deep()));
            style.Triggers.Add(selected);
            return style;
        }
        private static DataGrid NewTable()
        {
            var table=new DataGrid {
                AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,
                IsReadOnly=true,HeadersVisibility=DataGridHeadersVisibility.Column,
                EnableRowVirtualization=true,EnableColumnVirtualization=true,
                Background=PanelBrush(),BorderBrush=StrokeBrush(),BorderThickness=new Thickness(1),
                RowHeight=35,ColumnHeaderHeight=39,FontSize=14,GridLinesVisibility=DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush=StrokeBrush(),SelectionMode=DataGridSelectionMode.Extended,
                SelectionUnit=DataGridSelectionUnit.FullRow
            };
            table.SetResourceReference(Control.ForegroundProperty,"TextBrush");
            ScrollViewer.SetHorizontalScrollBarVisibility(table,ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(table,ScrollBarVisibility.Auto);
            return table;
        }
        private void AddPreviewField(string name,string current,string suggested,string action)
        {
            comparisonRows.Add(new PreviewFieldRow{Field=name,Current=current,Suggested=suggested,Decision=action});
        }
        private static string ShortText(string source,int limit=220)
        {
            if(String.IsNullOrWhiteSpace(source))return "(empty)";
            var flat=System.Text.RegularExpressions.Regex.Replace(source,@"<[^>]*>"," ");
            flat=System.Net.WebUtility.HtmlDecode(flat).Replace('\r',' ').Replace('\n',' ').Trim();
            return flat.Length>limit?flat.Substring(0,limit)+"…":flat;
        }
        private void ShowFullComparison()
        {
            var row=grid==null?null:grid.SelectedItem as CatalogViewRow;
            if(row==null){api.Dialogs.ShowMessage("Select a catalog row first.","Smart Metadata Matcher");return;}
            var detail=api.Dialogs.CreateWindow(new WindowCreationOptions{ShowMinimizeButton=false});
            detail.Title="Smart Metadata Matcher — Current vs. suggested metadata: "+row.CurrentName;
            detail.Width=1160;detail.Height=760;detail.MinWidth=710;detail.MinHeight=460;
            detail.Owner=window;detail.WindowStartupLocation=WindowStartupLocation.CenterOwner;
            var layout=new DockPanel{Margin=new Thickness(12),Background=Deep()};
            var lead=Caption("Drag any column boundary, resize the window, or select a field below to inspect and copy its complete current/suggested text. This preview makes no changes.",13);
            DockPanel.SetDock(lead,Dock.Top);layout.Children.Add(lead);
            var bottom=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right};
            DockPanel.SetDock(bottom,Dock.Bottom);layout.Children.Add(bottom);
            Button close;AddButton(bottom,"Close",()=>detail.Close(),out close);
            var content=new Grid();
            content.RowDefinitions.Add(new RowDefinition{Height=new GridLength(5,GridUnitType.Star),MinHeight=150});
            content.RowDefinitions.Add(new RowDefinition{Height=new GridLength(7)});
            content.RowDefinitions.Add(new RowDefinition{Height=new GridLength(4,GridUnitType.Star),MinHeight=130});
            var records=NewTable();records.RowHeight=42;
            records.Columns.Add(new DataGridTextColumn{Header="Field",Binding=new Binding("Field"),Width=135});
            records.Columns.Add(new DataGridTextColumn{Header="Current Playnite value",Binding=new Binding("Current"),Width=new DataGridLength(2,DataGridLengthUnitType.Star)});
            records.Columns.Add(new DataGridTextColumn{Header="Suggested / available from IGDB",Binding=new Binding("Suggested"),Width=new DataGridLength(2,DataGridLengthUnitType.Star)});
            records.Columns.Add(new DataGridTextColumn{Header="Action",Binding=new Binding("Decision"),Width=130});
            records.ItemsSource=comparisonRows.ToList();Grid.SetRow(records,0);content.Children.Add(records);
            var splitter=new GridSplitter{Height=7,HorizontalAlignment=HorizontalAlignment.Stretch,
                ResizeDirection=GridResizeDirection.Rows,ResizeBehavior=GridResizeBehavior.PreviousAndNext,
                ShowsPreview=true,Background=StrokeBrush(),ToolTip="Drag to expand full field values"};
            Grid.SetRow(splitter,1);content.Children.Add(splitter);
            var details=new Grid();
            details.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star),MinWidth=220});
            details.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(7)});
            details.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star),MinWidth=220});
            var currentBox=new TextBox{IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,
                VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,
                FontSize=13,Margin=new Thickness(0,0,4,0),ToolTip="Current value — select and copy text"};
            var suggestedBox=new TextBox{IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,
                VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,
                FontSize=13,Margin=new Thickness(4,0,0,0),ToolTip="Proposed or available value — select and copy text"};
            Grid.SetColumn(currentBox,0);Grid.SetColumn(suggestedBox,2);
            details.Children.Add(currentBox);details.Children.Add(suggestedBox);
            var splitColumns=new GridSplitter{Width=7,HorizontalAlignment=HorizontalAlignment.Stretch,
                ResizeDirection=GridResizeDirection.Columns,ResizeBehavior=GridResizeBehavior.PreviousAndNext,
                ShowsPreview=true,Background=StrokeBrush()};
            Grid.SetColumn(splitColumns,1);details.Children.Add(splitColumns);
            Grid.SetRow(details,2);content.Children.Add(details);
            records.SelectionChanged+=(s,e)=>{
                var field=records.SelectedItem as PreviewFieldRow;
                currentBox.Text=field==null?"Select a field above.":field.Current;
                suggestedBox.Text=field==null?"Select a field above.":field.Suggested;
            };
            if(records.Items.Count>0)records.SelectedIndex=0;
            layout.Children.Add(content);detail.Content=layout;detail.ShowDialog();
        }
        internal void Show()
        {
            window=api.Dialogs.CreateWindow(new WindowCreationOptions{ShowMinimizeButton=false});
            window.Title="Smart Metadata Matcher " + typeof(CatalogWindow).Assembly.GetName().Version.ToString(3) + " — private preview";
            window.Width=1610;window.Height=935;window.MinWidth=1120;window.MinHeight=675;
            var owner=api.Dialogs.GetCurrentAppWindow();
            if(owner!=null && owner!=window)window.Owner=owner;
            window.WindowStartupLocation=WindowStartupLocation.CenterOwner;
            var root=new DockPanel{Margin=new Thickness(8),Background=Deep()};

            var top=new StackPanel{Margin=new Thickness(4,2,4,0)};
            DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
            // Two-column header: calm identity/status hierarchy, never expose the secret.
            var header=new Grid{Margin=new Thickness(0,0,0,10)};
            header.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            top.Children.Add(header);
            var banner=new StackPanel{Orientation=Orientation.Horizontal};
            banner.Children.Add(Icon("\uE7FC",34,Accent()));
            var bannerText=new StackPanel();
            var heading=Caption("Smart Metadata Matcher",24);
            heading.FontWeight=FontWeights.SemiBold;heading.Margin=new Thickness(0,0,0,0);
            bannerText.Children.Add(heading);
            var sub=Caption("Automated catalog enrichment  •  Safe, reviewable Playnite updates",13);
            sub.Foreground=Muted();sub.Margin=new Thickness(0,0,0,0);
            bannerText.Children.Add(sub);banner.Children.Add(bannerText);
            Grid.SetColumn(banner,0);header.Children.Add(banner);

            clientId=new TextBox();secret=new PasswordBox();
            var settingsRow=new StackPanel{Orientation=Orientation.Horizontal,
                HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
            Grid.SetColumn(settingsRow,1);header.Children.Add(settingsRow);
            credentialStatus=Caption("IGDB credentials not configured",12);
            credentialStatus.Foreground=Muted();credentialStatus.Margin=new Thickness(0,3,11,0);
            settingsRow.Children.Add(credentialStatus);
            Button settings;
            AddButton(settingsRow,"IGDB settings",ShowCredentialSettings,out settings);
            Decorate(settings,"\uE713");
            try
            {
                var stored=credentialStore.Load();
                clientId.Text=stored.ClientId??"";secret.Password=stored.ClientSecret??"";
                RefreshCredentialStatus();
            }
            catch(Exception ex){
                credentialStatus.Text="IGDB credentials need attention";
                api.Dialogs.ShowMessage(ex.Message,"Smart Metadata Matcher");
            }

            // Every-day actions left; maintenance and diagnostics right.
            var toolbar=new Grid{Margin=new Thickness(0,0,0,6)};
            toolbar.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            toolbar.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            top.Children.Add(toolbar);
            var actions=new WrapPanel{VerticalAlignment=VerticalAlignment.Center};
            Grid.SetColumn(actions,0);toolbar.Children.Add(actions);
            AddButton(actions,"Scan games",StartScan,out scanButton);Decorate(scanButton,"\uE72C");
            // Native ComboBox matches the All platforms filter; no custom menu.
            actions.Children.Add(Caption("Scan size",12));
            scanSize=new ComboBox{Width=125,Height=32,FontSize=13,Margin=new Thickness(8,0,12,4)};
            scanSize.Items.Add("20 games");scanSize.Items.Add("50 games");
            scanSize.Items.Add("All remaining");scanSize.SelectedIndex=1;
            scanSize.ToolTip="Choose how many remaining games to scan; scanning is read-only.";
            actions.Children.Add(scanSize);
            useCatalogTitle=new CheckBox{Content="Clean titles on import",IsChecked=true,
                VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,12,4),FontSize=12};
            useCatalogTitle.ToolTip="Rename only Playnite display titles during approved imports; ROM files and launch paths are untouched.";
            useCatalogTitle.Checked+=(s,e)=>UpdatePreview();
            useCatalogTitle.Unchecked+=(s,e)=>UpdatePreview();
            actions.Children.Add(useCatalogTitle);
            AddButton(actions,"Cancel",Cancel,out cancelButton);
            cancelButton.IsEnabled=false;cancelButton.Visibility=Visibility.Collapsed;

            // Legacy testing controls still exist for compatibility but are not primary UI.
            var legacyActions=new WrapPanel{Visibility=Visibility.Collapsed};
            top.Children.Add(legacyActions);
            AddButton(legacyActions,"Apply one (legacy preview)",ApplySelected,out applyButton);
            AddButton(legacyActions,"Apply marked (legacy preview)",ApplyMarked,out batchButton);
            AddButton(legacyActions,"Auto-plan (legacy preview)",AutoPlan,out autoBatchButton);

            var auxiliary=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};
            Grid.SetColumn(auxiliary,1);toolbar.Children.Add(auxiliary);
            AddButton(auxiliary,"Undo",UndoLastMetadata,out undoButton);Decorate(undoButton,"\uE7A7");
            Button export,close;
            AddButton(auxiliary,"Export CSV",Export,out export);Decorate(export,"\uE8A5");
            Button tools=null;
            AddButton(auxiliary,"Tools  ▾",()=>ShowTools(tools),out tools);
            Decorate(tools,"\uE713");
            tools.ToolTip="Diagnostics, title review, history and legacy supervised testing";
            AddButton(auxiliary,"Close",()=>window.Close(),out close);
            summary=Caption("Scan games, review matches, then approve only the updates you want. Scanning does not change Playnite.",12);
            summary.Foreground=Muted();summary.Margin=new Thickness(1,2,0,9);top.Children.Add(summary);
            var divider=new Border{BorderBrush=StrokeBrush(),BorderThickness=new Thickness(0,0,0,1),
                Margin=new Thickness(0,0,0,3)};top.Children.Add(divider);

            var tabs=new WrapPanel{Margin=new Thickness(0,0,0,9)};top.Children.Add(tabs);
            resultView=new ComboBox{Visibility=Visibility.Collapsed};
            resultView.Items.Add("Active queue");resultView.Items.Add("Completed");
            resultView.Items.Add("Needs review");resultView.Items.Add("Ignored");resultView.Items.Add("All results");
            resultView.SelectionChanged+=(s,e)=>RefreshVisibility();resultView.SelectedIndex=0;
            MakeTab("Active queue",0,tabs);MakeTab("Completed",1,tabs);
            MakeTab("Needs review",2,tabs);MakeTab("Ignored",3,tabs);MakeTab("All results",4,tabs);

            var filterBar=new Grid{Margin=new Thickness(0,0,0,9)};
            filterBar.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            filterBar.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            top.Children.Add(filterBar);
            var filters=new WrapPanel();Grid.SetColumn(filters,0);filterBar.Children.Add(filters);
            AddButton(filters,"Select highlighted",ToggleHighlightedSelection,out selectionToggleButton);
            selectionToggleButton.ToolTip="Works on highlighted rows (Ctrl/Shift to highlight several). Checked games are selected for the next import.";
            AddButton(filters,"Select visible ready",ToggleVisibleReadySelection,out visibleToggleButton);
            visibleToggleButton.ToolTip="Select or deselect eligible games currently visible after filtering.";
            AddButton(filters,"Clear selection",()=>{
                foreach(var r in results)r.Marked=false;
                grid.Items.Refresh();UpdateSelectionActions();
            },out clearImportSelectionButton);
            clearImportSelectionButton.ToolTip="Uncheck every game selected for import. Does not delete or ignore games.";
            AddButton(filters,"Ignore highlighted",ToggleIgnored,out ignoreHighlightedButton);
            ignoreHighlightedButton.ToolTip="Ignore or restore the highlighted game. Only unresolved games can be ignored.";
            AddButton(filters,"Review & apply",ShowPlanReview,out reviewSelectionButton);
            Decorate(reviewSelectionButton,"\uE896",true);
            reviewSelectionButton.ToolTip="Review selected games and approve proposed Playnite metadata changes.";
            reviewSelectionButton.Foreground=Deep();
            var quickFilters=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};
            Grid.SetColumn(quickFilters,1);filterBar.Children.Add(quickFilters);
            quickFilters.Children.Add(Icon("\uE721",15));
            catalogSearch=new TextBox{Width=205,Height=32,FontSize=13,Margin=new Thickness(0,0,12,4),
                ToolTip="Filter by title, platform or IGDB ID"};
            catalogSearch.TextChanged+=(s,e)=>RefreshVisibility();quickFilters.Children.Add(catalogSearch);
            platformFilter=new ComboBox{Width=158,Height=32,Margin=new Thickness(0,0,0,4),FontSize=13};
            platformFilter.Items.Add("All platforms");platformFilter.SelectedIndex=0;
            platformFilter.SelectionChanged+=(s,e)=>RefreshVisibility();quickFilters.Children.Add(platformFilter);
            // The entire preview pane and the history panel can be resized by dragging splitters.
            var content=new Grid{Background=Deep()};
            content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(65,GridUnitType.Star),MinWidth=540});
            content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(7)});
            content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(35,GridUnitType.Star),MinWidth=410});
            content.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            root.Children.Add(content);
            var left=new Grid{Margin=new Thickness(0,0,2,0)};
            left.RowDefinitions.Add(new RowDefinition{Height=new GridLength(72,GridUnitType.Star),MinHeight=235});
            left.RowDefinitions.Add(new RowDefinition{Height=new GridLength(7)});
            historyRow=new RowDefinition{Height=GridLength.Auto,MinHeight=0};
            left.RowDefinitions.Add(historyRow);
            Grid.SetColumn(left,0);content.Children.Add(left);

            grid=NewTable();grid.FrozenColumnCount=2;
            var toggle=new FrameworkElementFactory(typeof(CheckBox));
            toggle.SetBinding(CheckBox.IsCheckedProperty,new Binding("Marked"){
                Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
            toggle.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);
            grid.Columns.Add(new DataGridTemplateColumn{Header="Mark",Width=54,
                CellTemplate=new DataTemplate{VisualTree=toggle},IsReadOnly=true});
            Column("Current title","CurrentName",235);
            Column("Platform","Platform",155);
            Column("Best IGDB candidate","Candidate",245);
            Column("Result","Status",128);
            ((DataGridTextColumn)grid.Columns[4]).ElementStyle=CandidateStatusStyle();
            Column("Score*","MatchScore",72);
            Column("IGDB ID","IgdbId",90);
            Column("Available fields","AvailableMetadata",152);
            Column("Year","ReleaseYear",62);
            Column("Other candidates","Alternatives",235);
            Column("Reason","Evidence",260);
            // Flexible title columns keep the day-to-day table readable without
            // permanently dedicating horizontal space to diagnostics.
            grid.Columns[1].Width=new DataGridLength(1.05,DataGridLengthUnitType.Star);
            grid.Columns[1].MinWidth=160;
            grid.Columns[3].Width=new DataGridLength(1.15,DataGridLengthUnitType.Star);
            grid.Columns[3].MinWidth=160;
            grid.Columns[2].Width=154;
            grid.Columns[4].Width=128;
            grid.Columns[7].Width=136;
            UpdateDiagnosticColumns();
            grid.SelectionChanged+=(s,e)=>{UpdatePreview();UpdateSelectionActions();};
            // Checkbox clicks change the import selection, separate from highlighted rows.
            // Defer until the WPF binding has committed the changed checkbox value.
            grid.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
                new RoutedEventHandler((s,e)=>{
                    if(!(e.OriginalSource is CheckBox))return;
                    window.Dispatcher.BeginInvoke(new Action(()=>UpdateSelectionActions()));
                }));
            Grid.SetRow(grid,0);left.Children.Add(grid);
            historySplitter=new GridSplitter{Height=7,HorizontalAlignment=HorizontalAlignment.Stretch,
                VerticalAlignment=VerticalAlignment.Stretch,ResizeDirection=GridResizeDirection.Rows,
                ResizeBehavior=GridResizeBehavior.PreviousAndNext,ShowsPreview=true,
                Background=StrokeBrush(),ToolTip="Drag to resize recent history"};
            Grid.SetRow(historySplitter,1);left.Children.Add(historySplitter);
            var historyLayout=new DockPanel{Margin=new Thickness(0,3,0,0)};
            Grid.SetRow(historyLayout,2);left.Children.Add(historyLayout);
            var historyHeading=new DockPanel{Margin=new Thickness(1,2,0,4)};
            DockPanel.SetDock(historyHeading,Dock.Top);historyLayout.Children.Add(historyHeading);
            var title=Caption("◷  Recent updates",14);
            title.FontWeight=FontWeights.SemiBold;
            Button fullHistory;AddButton(historyHeading,"View full history...",ShowFullHistory,out fullHistory);
            Decorate(fullHistory,"\uE8A7");DockPanel.SetDock(fullHistory,Dock.Right);
            // Primary history actions remain visible; there is no nested menu.
            // The first button reverses committed changes; the second only hides
            // history rows. Their distinct labels prevent accidental data loss.
            AddButton(historyHeading,"Restore changes",()=>
                UndoSelectedHistory(historyGrid.SelectedItems.OfType<HistoryRow>().ToList()),out restoreHistoryButton);
            restoreHistoryButton.ToolTip="Restore earlier Playnite metadata using undo records. Only the newest consecutive operations can be reversed.";
            DockPanel.SetDock(restoreHistoryButton,Dock.Right);
            AddButton(historyHeading,"Remove from recent",()=>
                HideSelectedHistory(historyGrid.SelectedItems.OfType<HistoryRow>().ToList(),null),out removeRecentButton);
            removeRecentButton.ToolTip="Hide selected history rows without deleting games, metadata, or undo records. Restore in Full history.";
            DockPanel.SetDock(removeRecentButton,Dock.Right);
            historyHeading.Children.Add(title);
            restoreHistoryButton.IsEnabled=false;
            removeRecentButton.IsEnabled=false;
            historyGrid=NewTable();historyGrid.SelectionMode=DataGridSelectionMode.Extended;
            historyGrid.SelectionUnit=DataGridSelectionUnit.FullRow;
            historyGrid.ToolTip="Click, Ctrl-click or Shift-click to select operations. Undo only supports the latest consecutive operations.";
            historyGrid.RowHeight=27;historyGrid.ColumnHeaderHeight=30;
            historyGrid.Columns.Add(new DataGridTextColumn{Header="Date",Binding=new Binding("When"),Width=146});
            historyGrid.Columns.Add(new DataGridTextColumn{Header="Game",Binding=new Binding("Game"),Width=225});
            historyGrid.Columns.Add(new DataGridTextColumn{Header="Applied fields",Binding=new Binding("Fields"),
                Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
            historyGrid.Columns.Add(new DataGridTextColumn{Header="Result",Binding=new Binding("Result"),Width=97,
                ElementStyle=ResultStyle()});
            historyGrid.SelectionChanged+=(s,e)=>{
                var selected=historyGrid.SelectedItems.OfType<HistoryRow>().ToList();
                title.Text=selected.Count==0?"◷  Recent updates":
                    "◷  Recent updates ("+selected.Count+" selected)";
                if(restoreHistoryButton!=null)
                    restoreHistoryButton.IsEnabled=!busy && selected.Any(x=>x.Result=="Applied");
                if(removeRecentButton!=null)
                    removeRecentButton.IsEnabled=!busy && selected.Any(x=>!x.Hidden);
            };
            historyGrid.MouseDoubleClick+=(s,e)=>{
                var selected=historyGrid.SelectedItem as HistoryRow;if(selected==null)return;
                resultView.SelectedIndex=1;
                var match=results.FirstOrDefault(r=>r.GameId==selected.GameId&&r.IsCompleted);
                if(match!=null){grid.SelectedItem=match;grid.ScrollIntoView(match);}
            };
            historyLayout.Children.Add(historyGrid);
            emptyHistory=Caption("No imports yet. Completed updates and undoable changes appear here.",12);
            emptyHistory.Foreground=Muted();
            historyLayout.Children.Add(emptyHistory);

            var mainSplitter=new GridSplitter{Width=7,HorizontalAlignment=HorizontalAlignment.Stretch,
                VerticalAlignment=VerticalAlignment.Stretch,ResizeDirection=GridResizeDirection.Columns,
                ResizeBehavior=GridResizeBehavior.PreviousAndNext,ShowsPreview=true,
                Background=StrokeBrush(),ToolTip="Drag horizontally to widen metadata preview"};
            Grid.SetColumn(mainSplitter,1);content.Children.Add(mainSplitter);

            var previewRoot=new DockPanel{Margin=new Thickness(3,0,0,0)};
            var previewFrame=Frame(previewRoot,new Thickness(12,10,12,8));
            Grid.SetColumn(previewFrame,2);content.Children.Add(previewFrame);
            var previewHead=new StackPanel{Orientation=Orientation.Vertical};
            DockPanel.SetDock(previewHead,Dock.Top);previewRoot.Children.Add(previewHead);
            previewTitle=Caption("Metadata preview",17);previewTitle.FontWeight=FontWeights.SemiBold;
            previewTitle.Foreground=Accent();previewHead.Children.Add(previewTitle);
            previewState=Caption("Select a matched game to inspect proposed changes.",12);
            previewState.Foreground=Muted();previewHead.Children.Add(previewState);
            var previewFoot=new StackPanel {Margin=new Thickness(0,7,0,0)};
            DockPanel.SetDock(previewFoot,Dock.Bottom);previewRoot.Children.Add(previewFoot);
            previewSummary=Caption("Identity verification is required before any metadata changes.",12);
            previewSummary.Foreground=Muted();
            previewFoot.Children.Add(Frame(previewSummary,new Thickness(9,6,9,6)));
            var previewActions=new WrapPanel{Margin=new Thickness(0,9,0,0)};
            previewFoot.Children.Add(previewActions);
            Button apply,skip,inspect;
            AddButton(previewActions,"Review / Apply",ShowPlanReview,out apply);Decorate(apply,"\uE896",true);
            AddButton(previewActions,"Skip / Ignore",ToggleIgnored,out skip);Decorate(skip,"\uE711");
            AddButton(previewActions,"Full details",ShowFullComparison,out inspect);Decorate(inspect,"\uE8A7");

            var previewScroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
            previewRoot.Children.Add(previewScroll);
            var previewLayout=new StackPanel{Margin=new Thickness(2,4,2,5)};
            previewScroll.Content=previewLayout;
            // Artwork and the useful before/after summary are the primary preview.
            var gallery=new WrapPanel{Margin=new Thickness(0,5,0,10)};
            previewLayout.Children.Add(gallery);
            var coverPanel=new StackPanel{Margin=new Thickness(0,0,13,0)};
            previewCover=new Image{Height=168,Width=128,Stretch=Stretch.Uniform};
            coverPanel.Children.Add(previewCover);coverPanel.Children.Add(Caption("Cover image",12));gallery.Children.Add(coverPanel);
            var iconPanel=new StackPanel{Margin=new Thickness(0,0,13,0)};
            previewIcon=new Image{Height=100,Width=92,Stretch=Stretch.Uniform};
            iconPanel.Children.Add(previewIcon);iconPanel.Children.Add(Caption("Icon",12));gallery.Children.Add(iconPanel);
            var backgroundPanel=new StackPanel();
            previewBackground=new Image{Height=110,Width=174,Stretch=Stretch.Uniform};
            backgroundPanel.Children.Add(previewBackground);backgroundPanel.Children.Add(Caption("Background",12));gallery.Children.Add(backgroundPanel);
            previewDetails=Caption("Select a game to see current and proposed metadata.",12);
            previewDetails.Foreground=Muted();previewLayout.Children.Add(previewDetails);

            var changeCard=new StackPanel{Margin=new Thickness(0,9,0,10)};
            previewLayout.Children.Add(Frame(changeCard,new Thickness(10,9,10,8)));
            previewChangeHeading=Caption("Potential changes",15);
            previewChangeHeading.FontWeight=FontWeights.SemiBold;
            previewChangeHeading.Foreground=Accent();changeCard.Children.Add(previewChangeHeading);
            previewHighlights=new StackPanel();changeCard.Children.Add(previewHighlights);

            AddButton(previewLayout,"Inspect additional IGDB fields (read-only)",InspectIgdbFields,out inspectFieldsButton);
            Decorate(inspectFieldsButton,"\uE721");
            inspectFieldsButton.ToolTip="Exact-ID lookup for optional release, genre, company and series fields; no Playnite changes.";

            var fullDetails=new Expander{Header="Detailed field comparison and diagnostics  ▾",
                IsExpanded=false,Foreground=Muted(),FontSize=13,Margin=new Thickness(0,0,0,8)};
            var detailsBody=new StackPanel{Margin=new Thickness(0,9,0,4)};
            fullDetails.Content=detailsBody;previewLayout.Children.Add(fullDetails);
            previewComparison=NewTable();previewComparison.RowHeight=37;
            previewComparison.ColumnHeaderHeight=32;previewComparison.MinHeight=180;
            previewComparison.Height=300;previewComparison.FontSize=12;
            previewComparison.Columns.Add(new DataGridTextColumn{Header="Field",Binding=new Binding("Field"),Width=95});
            previewComparison.Columns.Add(new DataGridTextColumn{Header="Current",Binding=new Binding("Current"),Width=145});
            previewComparison.Columns.Add(new DataGridTextColumn{Header="Suggested",Binding=new Binding("Suggested"),Width=200});
            previewComparison.Columns.Add(new DataGridTextColumn{Header="Handling",Binding=new Binding("Decision"),Width=115});
            previewComparison.ItemsSource=comparisonRows;
            previewComparison.MouseDoubleClick+=(s,e)=>ShowFullComparison();
            detailsBody.Children.Add(previewComparison);
            previewDiagnostics=Caption("Matching diagnostics appear here after selecting a game.",11);
            previewDiagnostics.Foreground=Muted();detailsBody.Children.Add(previewDiagnostics);
            window.Content=root;
            var completed=enrichmentJournal.Committed();
            var completedIds=new HashSet<Guid>();
            foreach(var entry in completed)
            {
                if(completedIds.Contains(entry.GameId))continue;
                var live=api.Database.Games[entry.GameId];
                if(live==null || !EnrichmentPlan.IsFullyApplied(entry,live.Name,live.SortingName,
                    live.Description,live.CoverImage,live.Icon,live.BackgroundImage) ||
                    !EnrichmentPlan.ExtraFieldsApplied(entry,GameExtrasSnapshot.ReleaseValue(live),
                        live.GenreIds,live.DeveloperIds,live.PublisherIds,live.SeriesIds)) continue;
                completedIds.Add(entry.GameId);
                results.Add(new CatalogViewRow {
                    GameId=entry.GameId,CurrentName=live.Name,
                    Platform=live.Platforms==null?"":String.Join(", ",live.Platforms.Select(p=>p.Name)),
                    Candidate=entry.CatalogTitle,Status="Completed",PreviousStatus="Strong candidate",
                    IsCompleted=true,FromHistory=true,IgdbId=entry.IgdbId.ToString(),
                    Evidence="Verified previously imported metadata (see history)"
                });
            }
            ignoredIds=ignoredStore.Load();
            foreach(var ignored in api.Database.Games.Where(g=>g!=null && ignoredIds.Contains(g.Id) &&
                     !completedIds.Contains(g.Id)))
            {
                results.Add(new CatalogViewRow { GameId=ignored.Id,CurrentName=ignored.Name,
                    Platform=ignored.Platforms==null?"":String.Join(", ",ignored.Platforms.Select(p=>p.Name)),
                    Candidate="(ignored)",Status="Ignored",IsIgnored=true,FromHistory=true,
                    Evidence="User excluded from catalog matching" });
            }
            grid.ItemsSource=results;
            queue=api.Database.Games.Where(g=>g!=null && !String.IsNullOrWhiteSpace(g.Name) &&
                    !completedIds.Contains(g.Id) && !ignoredIds.Contains(g.Id))
                .Select(g=>new CatalogInput { Id=g.Id,Name=g.Name,
                    Platform=g.Platforms==null?"":String.Join(", ",g.Platforms.Select(p=>p.Name)),
                    MissingCount=(String.IsNullOrWhiteSpace(g.CoverImage)?1:0)+
                                 (String.IsNullOrWhiteSpace(g.BackgroundImage)?1:0)+
                                 (String.IsNullOrWhiteSpace(g.Description)?1:0)})
                .Where(x=>x.MissingCount>0).ToList();
            excludedTesting=queue.Count(x=>CatalogCore.IsTestingOnly(x.Name));
            queue=queue.Where(x=>!CatalogCore.IsTestingOnly(x.Name))
                .OrderByDescending(x=>x.MissingCount)
                .ThenBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToList();
            foreach(var platform in api.Database.Platforms.Where(p=>p!=null)
                .Select(p=>p.Name).Where(p=>!String.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))
                platformFilter.Items.Add(platform);
            summary.Text=queue.Count+" eligible games; "+completedIds.Count+" completed; "+ignoredIds.Count+
                " ignored; "+excludedTesting+" test clients excluded. Scanning is read-only.";
            RefreshHistory();RefreshVisibility();
            window.Closed+=(s,e)=>Cancel();
            window.ShowDialog();
        }
        private void RefreshCredentialStatus()
        {
            if(credentialStatus==null)return;
            credentialStatus.Text=String.IsNullOrWhiteSpace(clientId.Text) ||
                String.IsNullOrWhiteSpace(secret.Password)
                ? "IGDB credentials not configured"
                : "IGDB credentials configured locally (connection not tested)";
        }

        private void ShowCredentialSettings()
        {
            if(busy)return;
            var dialog=new Window {Title="Smart Metadata Matcher — IGDB settings",
                Width=600,Height=340,MinWidth=480,MinHeight=290,Owner=window,
                WindowStartupLocation=WindowStartupLocation.CenterOwner,
                Background=Deep(),ResizeMode=ResizeMode.CanResize};
            var body=new DockPanel {Margin=new Thickness(18)};
            var fields=new StackPanel();body.Children.Add(fields);
            fields.Children.Add(Caption("Twitch developer Client ID",13));
            var id=new TextBox {Text=clientId.Text,MinHeight=32,FontSize=14,
                Margin=new Thickness(0,0,0,11)};
            fields.Children.Add(id);
            fields.Children.Add(Caption("Client Secret",13));
            var password=new PasswordBox {Password=secret.Password,MinHeight=32,
                Margin=new Thickness(0,0,0,10)};
            fields.Children.Add(password);
            var note=Caption("Saved for the current Windows user using existing encrypted local storage. " +
                "No connection check occurs when saving. Never share credentials in logs or screenshots.",12);
            note.Foreground=Muted();fields.Children.Add(note);
            var footer=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right,
                Margin=new Thickness(0,14,0,0)};
            DockPanel.SetDock(footer,Dock.Bottom);
            body.Children.Insert(0,footer);
            Button save,cancel;
            AddButton(footer,"Save settings",()=>{
                if(String.IsNullOrWhiteSpace(id.Text) || String.IsNullOrWhiteSpace(password.Password))
                {api.Dialogs.ShowMessage("Both IGDB credential fields are required.",
                    "Smart Metadata Matcher");return;}
                try
                {
                    // Persist before updating active credentials, so a failed
                    // encryption/save never silently changes the running state.
                    credentialStore.Save(id.Text,password.Password);
                    clientId.Text=id.Text;secret.Password=password.Password;
                    RefreshCredentialStatus();
                    summary.Text="IGDB credentials saved locally. Connection has not been tested.";
                    dialog.DialogResult=true;
                }
                catch(Exception ex){api.Dialogs.ShowErrorMessage(ex.Message,"Could not save IGDB credentials");}
            },out save);Decorate(save,"\uE74E",true);
            AddButton(footer,"Cancel",()=>{dialog.DialogResult=false;},out cancel);
            dialog.Content=body;dialog.ShowDialog();
        }

        // Identity preview itself is read-only. A separate, explicitly confirmed
        // private testing action may dispatch marked, strictly eligible rows
        // through the EXISTING journaled batch writer. This is not the v0.5 runner.
        private void ShowPlanReview()
        {
            if(busy)return;
            if(results.Count==0)
            {
                api.Dialogs.ShowMessage("Scan at least one batch to build a review plan. " +
                    "Scanning and this preview are read-only.","Smart Metadata Matcher");
                return;
            }
            var library=api.Database.Games.Where(g=>g!=null).Select(g=>Tuple.Create(
                g.Id,g.Name,g.Platforms==null?"":String.Join(", ",g.Platforms.Select(p=>p.Name)))).ToList();
            var candidates=results.Select(x=>new EnrichmentCandidate {
                GameId=x.GameId,CurrentName=x.CurrentName,Platform=x.Platform,
                CandidateTitle=x.Candidate,CatalogId=x.IgdbId,Status=x.Status,
                StrictEvidence=x.AutoPlanEligible,Completed=x.IsCompleted,Ignored=x.IsIgnored,
                FromHistory=x.FromHistory,Marked=x.Marked,
                PriorFailure=(x.Evidence??"").StartsWith("Apply failed:",StringComparison.Ordinal)
            }).ToList();
            var decisions=EnrichmentJobPlanner.Create(candidates,library,useCatalogTitle.IsChecked==true,false);
            var lines=new List<PlanPreviewRow>();
            for(int i=0;i<decisions.Count;i++)
            {
                var d=decisions[i];var r=results[i];
                lines.Add(new PlanPreviewRow {
                    SourceRow=r,Game=r.CurrentName??"",Catalog=r.Candidate??"",
                    Platform=r.Platform??"",CatalogId=r.IgdbId??"",
                    IdentityStatus=d.ReadyForPreflight?"Ready — verify":
                        d.Code=="Completed"?"Completed":d.Code=="Ignored"?"Ignored":"Needs review",
                    Explanation=d.Explanation,RenameHandling=d.ReadyForPreflight?d.RenameReason:"Not planned",
                    Ready=d.ReadyForPreflight,Archived=d.Code=="Completed"||d.Code=="Ignored"
                });
            }
            var ready=lines.Count(x=>x.Ready);
            var review=lines.Count(x=>!x.Ready&&!x.Archived);
            var archived=lines.Count(x=>x.Archived);
            Func<int> markedCount=()=>lines.Count(x=>x.Marked);
            var dialog=new Window {Title="Smart Metadata Matcher — Review matches",
                Width=1120,Height=700,MinWidth=720,MinHeight=450,Owner=window,
                WindowStartupLocation=WindowStartupLocation.CenterOwner,
                Background=Deep(),ResizeMode=ResizeMode.CanResize};
            var body=new DockPanel {Margin=new Thickness(14)};
            var heading=new StackPanel{Margin=new Thickness(0,0,0,10)};
            DockPanel.SetDock(heading,Dock.Top);body.Children.Add(heading);
            var title=Caption("Review matches and proposed changes",19);
            title.FontWeight=FontWeights.SemiBold;title.Foreground=Accent();heading.Children.Add(title);
            var counts=Caption("",14);
            Action updateCounts=()=>counts.Text=markedCount()+" selected for import  •  "+ready+
                " ready for review  •  "+review+" need attention  •  "+archived+" completed/ignored";
            updateCounts();
            heading.Children.Add(counts);
            var warning=Caption("Check each game's title, platform and edition before approving. " +
                "Ready means automated checks passed, not that the identity is guaranteed. " +
                "A backup and final approval are required; imports are still supervised.",12);
            warning.Foreground=Muted();heading.Children.Add(warning);
            var filters=new WrapPanel{Margin=new Thickness(0,0,0,8)};
            DockPanel.SetDock(filters,Dock.Top);body.Children.Add(filters);
            filters.Children.Add(Caption("Show",12));
            var view=new ComboBox {Width=225,Height=31,Margin=new Thickness(10,0,10,0)};
            view.Items.Add("Selected for import");view.Items.Add("Ready matches");
            view.Items.Add("Needs review");view.Items.Add("Completed / ignored");view.Items.Add("All matches");
            filters.Children.Add(view);
            var search=new TextBox {Width=210,Height=31,Margin=new Thickness(0,0,12,0),
                ToolTip="Find a Playnite game or IGDB candidate in this plan"};
            filters.Children.Add(search);
            var filterStatus=Caption("",12);filters.Children.Add(filterStatus);
            var table=NewTable();table.SelectionMode=DataGridSelectionMode.Single;
            // Only the checkbox column is editable; identity fields are read-only.
            table.IsReadOnly=false;
            var markFactory=new FrameworkElementFactory(typeof(CheckBox));
            markFactory.SetBinding(CheckBox.IsCheckedProperty,new Binding("Marked"){
                Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
            markFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);
            table.Columns.Add(new DataGridTemplateColumn {Header="Select",Width=57,
                CellTemplate=new DataTemplate{VisualTree=markFactory},IsReadOnly=false});
            table.Columns.Add(new DataGridTextColumn{Header="Current Playnite title",Binding=new Binding("Game"),Width=205,IsReadOnly=true});
            table.Columns.Add(new DataGridTextColumn{Header="Platform",Binding=new Binding("Platform"),Width=135,IsReadOnly=true});
            table.Columns.Add(new DataGridTextColumn{Header="Matched game (IGDB)",Binding=new Binding("Catalog"),Width=210,IsReadOnly=true});
            table.Columns.Add(new DataGridTextColumn{Header="Match status",Binding=new Binding("IdentityStatus"),Width=137,IsReadOnly=true});
            table.Columns.Add(new DataGridTextColumn{Header="Reason",Binding=new Binding("Explanation"),
                Width=new DataGridLength(1,DataGridLengthUnitType.Star),IsReadOnly=true});
            table.Columns.Add(new DataGridTextColumn{Header="Playnite title after import",Binding=new Binding("RenameHandling"),Width=220,IsReadOnly=true});
            var footer=new WrapPanel {Margin=new Thickness(0,10,0,0),
                HorizontalAlignment=HorizontalAlignment.Right};
            DockPanel.SetDock(footer,Dock.Bottom);body.Children.Add(footer);
            Button inspect=null,previewApply=null,close=null;
            Button markReady=null,clearMarked=null;
            Action updateSelection=()=>{
                updateCounts();
                if(inspect!=null)inspect.IsEnabled=table.SelectedItem is PlanPreviewRow;
                if(previewApply!=null){
                    previewApply.IsEnabled=markedCount()>0;
                    previewApply.Content="Apply selected ("+markedCount()+")...";
                }
                if(markReady!=null){
                    var readyRows=lines.Where(x=>x.Ready&&!x.Archived).ToList();
                    markReady.Content=readyRows.Count==0?"No ready matches":
                        readyRows.Any(x=>!x.Marked)?"Select all ready ("+readyRows.Count+")":
                        "Deselect all ready ("+readyRows.Count+")";
                    markReady.IsEnabled=readyRows.Count>0;
                }
                if(grid!=null)grid.Items.Refresh();
                UpdateSelectionActions();
            };
            Action refreshRows=()=>{
                var current=table.SelectedItem as PlanPreviewRow;
                IEnumerable<PlanPreviewRow> scope=lines;
                if(view.SelectedIndex==0)scope=scope.Where(x=>x.Marked);
                else if(view.SelectedIndex==1)scope=scope.Where(x=>x.Ready);
                else if(view.SelectedIndex==2)scope=scope.Where(x=>!x.Ready&&!x.Archived);
                else if(view.SelectedIndex==3)scope=scope.Where(x=>x.Archived);
                var query=search.Text.Trim();
                if(query.Length>0)scope=scope.Where(x=>
                    (x.Game??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0 ||
                    (x.Catalog??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0 ||
                    (x.Platform??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0 ||
                    (x.CatalogId??"").IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0);
                var visible=scope.ToList();
                table.ItemsSource=visible;
                table.SelectedItem=current!=null&&visible.Contains(current)?current:visible.FirstOrDefault();
                filterStatus.Text=visible.Count==0 ? "No games in this view" :
                    "Showing "+visible.Count+" (check boxes to mark)";
                updateSelection();
            };
            view.SelectionChanged+=(s,e)=>refreshRows();
            search.TextChanged+=(s,e)=>refreshRows();
            table.SelectionChanged+=(s,e)=>updateSelection();
            // Checkboxes update the same underlying marks as the main dashboard.
            // Refresh on the dispatcher after the binding has committed its value.
            table.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
                new RoutedEventHandler((s,e)=>{
                    if(!(e.OriginalSource is CheckBox))return;
                    dialog.Dispatcher.BeginInvoke(new Action(()=>refreshRows()));
                }));
            var selectionTools=new WrapPanel{Margin=new Thickness(0,2,0,8)};
            DockPanel.SetDock(selectionTools,Dock.Top);body.Children.Add(selectionTools);
            AddButton(selectionTools,"Select all ready",()=>{
                var readyRows=lines.Where(x=>x.Ready&&!x.Archived).ToList();
                bool select=readyRows.Any(x=>!x.Marked);
                foreach(var row in readyRows)row.Marked=select;
                refreshRows();
            },out markReady);
            AddButton(selectionTools,"Clear selection",()=>{
                foreach(var row in lines)row.Marked=false;
                refreshRows();
            },out clearMarked);
            Action focus=()=>{
                var row=table.SelectedItem as PlanPreviewRow;
                if(row==null){api.Dialogs.ShowMessage("No game selected. Choose a category with rows, " +
                    "then highlight a game to inspect.","Smart Metadata Matcher");return;}
                dialog.Close();
                resultView.SelectedIndex=4;
                catalogSearch.Text="";platformFilter.SelectedIndex=0;
                RefreshVisibility();
                if(grid.Items.Contains(row.SourceRow))
                {grid.SelectedItem=row.SourceRow;grid.ScrollIntoView(row.SourceRow);UpdatePreview();}
                else api.Dialogs.ShowMessage("The selected result is no longer visible. " +
                    "Rescan if its identity changed.","Smart Metadata Matcher");
            };
            AddButton(footer,"Inspect game",focus,out inspect);
            Decorate(inspect,"\uE8A7");
            AddButton(footer,"Apply selected (supervised test)...",()=>{
                var selected=lines.Where(x=>x.Marked).ToList();
                if(selected.Count==0){api.Dialogs.ShowMessage("Select at least one game using the checkboxes. " +
                    "Highlighting a row only opens its details.","Smart Metadata Matcher");return;}
                var blocked=selected.Where(x=>!x.Ready).ToList();
                // Preserve the earlier one-game manual path for a game whose IGDB
                // identity the user has personally verified. It is NOT automated.
                bool manualSingle=selected.Count==1 && blocked.Count==1 &&
                    !blocked[0].Archived && !blocked[0].SourceRow.IsIgnored &&
                    !blocked[0].SourceRow.IsCompleted &&
                    String.Equals(blocked[0].SourceRow.Status,"Strong candidate",StringComparison.Ordinal) &&
                    EnrichmentPlan.BatchEligible(blocked[0].SourceRow.Status,false,blocked[0].CatalogId);
                if(blocked.Count>0 && !manualSingle)
                {
                    api.Dialogs.ShowMessage("The marked selection includes games that are not strict-ready. " +
                        "Only strict-ready games can use the multi-game test path. " +
                        "For a manually reviewed candidate, mark exactly ONE Strong match whose " +
                        "IGDB identity, platform and edition you have checked yourself.",
                        "Smart Metadata Matcher");return;
                }
                // Temporary validation cap, not a user-facing batch architecture.
                // Increase only after recovery/accuracy tests; never silently truncate.
                if(selected.Count>5){api.Dialogs.ShowMessage("This private test preview currently limits " +
                    "each supervised import to five marked games. Unmark the others for this test. " +
                    "The production workflow will not expose separate one/five/25 buttons.",
                    "Smart Metadata Matcher");return;}
                var prompt=manualSingle ?
                    "MANUAL identity confirmation: this match is not eligible for strict automation. " +
                    "Only continue if you personally checked its IGDB ID, game title, platform and edition. " +
                    "A wrong identity could fill empty fields with wrong data. " :
                    "Every marked game passed the strict planning gate, but its identity is not guaranteed. ";
                var backup=api.Dialogs.ShowMessage(prompt+
                    "Confirm you have a recent backup of your Playnite library AND extension data. " +
                    "A separate approval will list every game before writes. " +
                    "Existing populated metadata and game files are not intentionally overwritten. " +
                    "Proceed to final review?","Private supervised import — backup required",
                    MessageBoxButton.YesNo,MessageBoxImage.Warning);
                if(backup!=MessageBoxResult.Yes)return;
                // Leave the modal review window before opening the existing
                // confirmation dialog, avoiding nested dialog ownership issues.
                var reviewedRows=selected.Select(x=>x.SourceRow).ToList();
                var excludedCount=lines.Count-reviewedRows.Count;
                dialog.Close();
                window.Dispatcher.BeginInvoke(new Action(()=>RunBatch(reviewedRows,!manualSingle,excludedCount)));
            },out previewApply);
            previewApply.Background=Accent();previewApply.BorderBrush=Accent();
            previewApply.Foreground=Deep();previewApply.FontWeight=FontWeights.SemiBold;
            previewApply.ToolTip="Review and apply checked games after backup confirmation and a final game-by-game approval.";
            AddButton(footer,"Close",()=>dialog.Close(),out close);
            table.MouseDoubleClick+=(s,e)=>focus();
            body.Children.Add(table);
            // Prioritize the user's marked subset. If no marks exist and no
            // strict-ready matches exist, show all rather than an empty list.
            view.SelectedIndex=markedCount()>0?0:ready>0?1:4;
            dialog.Content=body;dialog.ShowDialog();
        }

        private void UpdateDiagnosticColumns()
        {
            if(grid==null || grid.Columns.Count<11)return;
            var display=diagnosticColumnsVisible?Visibility.Visible:Visibility.Collapsed;
            foreach(var index in new[]{5,6,8,9,10})grid.Columns[index].Visibility=display;
        }

        private void UpdateTabs()
        {
            int active=results.Count(x=>!x.IsCompleted&&!x.IsIgnored&&x.Status=="Strong candidate");
            int completed=results.Count(x=>x.IsCompleted);
            int review=results.Count(x=>!x.IsCompleted&&!x.IsIgnored&&x.Status!="Strong candidate");
            int ignored=results.Count(x=>x.IsIgnored);
            var labels=new[]{"Active queue ("+active+")","Completed ("+completed+")",
                "Needs review ("+review+")","Ignored ("+ignored+")","All results ("+results.Count+")"};
            for(int i=0;i<tabButtons.Count;i++)
                SetTabContent(tabButtons[i],labels[i],i,i==resultView.SelectedIndex);
        }
        private List<HistoryRow> HistoryRows(int take,bool includeHidden=false)
        {
            var view=new List<HistoryRow>();
            foreach(var e in enrichmentJournal.AllEntries().Where(x=>x.Committed &&
                (includeHidden || !hiddenOperations.Contains(x.OperationId))).Take(take))
            {
                var fields=new List<string>();
                if(e.ChangedName)fields.Add("Name");
                if(e.ChangedSorting)fields.Add("Sorting");
                if(e.ChangedDescription)fields.Add("Description");
                if(e.ChangedCover)fields.Add("Cover");
                if(e.ChangedIcon)fields.Add("Icon");
                if(e.ChangedBackground)fields.Add("Background");
                if(e.ChangedRelease)fields.Add("Release year");
                if(e.ChangedGenres)fields.Add("Genres");
                if(e.ChangedDevelopers)fields.Add("Developers");
                if(e.ChangedPublishers)fields.Add("Publishers");
                if(e.ChangedSeries)fields.Add("Series");
                view.Add(new HistoryRow { OperationId=e.OperationId,GameId=e.GameId,Game=e.AfterName??e.BeforeName,
                    Fields=String.Join(", ",fields),
                    When=e.CommittedUtc==default(DateTime)?"Unknown (legacy)":
                        e.CommittedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    Result=e.Undone?"Undone":"Applied",Hidden=hiddenOperations.Contains(e.OperationId) });
            }
            return view;
        }
        private void RefreshHistory()
        {
            if(historyGrid==null)return;
            var rows=HistoryRows(10);
            historyGrid.ItemsSource=rows;
            bool hasHistory=rows.Count>0;
            historyGrid.Visibility=hasHistory?Visibility.Visible:Visibility.Collapsed;
            if(emptyHistory!=null)emptyHistory.Visibility=hasHistory?Visibility.Collapsed:Visibility.Visible;
            if(historySplitter!=null)historySplitter.Visibility=hasHistory?Visibility.Visible:Visibility.Collapsed;
            if(historyRow!=null)historyRow.Height=hasHistory?
                new GridLength(25,GridUnitType.Star):GridLength.Auto;
            // Empty history should not consume a third of the dashboard.
        }
        private void ShowFullHistory()
        {
            var detail=api.Dialogs.CreateWindow(new WindowCreationOptions { ShowMinimizeButton=false });
            detail.Title="Smart Metadata Matcher — Full operation history";
            detail.Width=950;detail.Height=610;detail.MinWidth=700;detail.MinHeight=400;
            detail.Owner=window;detail.WindowStartupLocation=WindowStartupLocation.CenterOwner;
            var root=new DockPanel {Margin=new Thickness(12)};
            var lead=Caption("Operations are retained for undo/recovery. Remove from recent only hides them; use Show hidden to restore. Ctrl/Shift selects multiple. Only newest consecutive applied operations can be undone.",13);
            DockPanel.SetDock(lead,Dock.Top);root.Children.Add(lead);
            var foot=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,8,0,0)};
            DockPanel.SetDock(foot,Dock.Bottom);root.Children.Add(foot);
            Button close;AddButton(foot,"Close",()=>detail.Close(),out close);
            var records=new DataGrid {
                AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,IsReadOnly=true,
                HeadersVisibility=DataGridHeadersVisibility.Column,EnableRowVirtualization=true,
                SelectionMode=DataGridSelectionMode.Extended,SelectionUnit=DataGridSelectionUnit.FullRow
            };
            var showHidden=new CheckBox{Content="Show hidden",Margin=new Thickness(6,8,10,0),VerticalAlignment=VerticalAlignment.Center};
            DockPanel.SetDock(showHidden,Dock.Top);
            root.Children.Add(showHidden);
            Action refreshFull=()=>{records.ItemsSource=HistoryRows(Int32.MaxValue,showHidden.IsChecked==true);};
            showHidden.Checked+=(s,e)=>refreshFull();
            showHidden.Unchecked+=(s,e)=>refreshFull();
            Button restoreInFull;AddButton(foot,"Restore selected",()=>{
                RestoreSelectedHistory(records.SelectedItems.OfType<HistoryRow>().ToList(),refreshFull);
            },out restoreInFull);
            Button hideInFull;AddButton(foot,"Remove from history view...",()=>{
                HideSelectedHistory(records.SelectedItems.OfType<HistoryRow>().ToList(),refreshFull);
            },out hideInFull);
            Button renameInFull;AddButton(foot,"Fix Playnite titles...",()=>{
                RenameSelectedHistoryTitles(records.SelectedItems.OfType<HistoryRow>().ToList());
                refreshFull();
            },out renameInFull);
            Button undoInFull;AddButton(foot,"Undo selected (latest first)",()=>{
                UndoSelectedHistory(records.SelectedItems.OfType<HistoryRow>().ToList());
                refreshFull();
            },out undoInFull);
            records.ToolTip="Ctrl-click and Shift-click select multiple history operations. Undo only the latest consecutive operations.";
            records.Columns.Add(new DataGridTextColumn {Header="Date",Binding=new Binding("When"),Width=150});
            records.Columns.Add(new DataGridTextColumn {Header="Game",Binding=new Binding("Game"),Width=275});
            records.Columns.Add(new DataGridTextColumn {Header="Fields applied",Binding=new Binding("Fields"),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
            records.Columns.Add(new DataGridTextColumn {Header="State",Binding=new Binding("Result"),Width=100,
                ElementStyle=ResultStyle()});
            records.Columns.Add(new DataGridTextColumn {Header="Visibility",Binding=new Binding("VisibilityLabel"),Width=90});
            refreshFull();
            root.Children.Add(records);detail.Content=root;detail.ShowDialog();
        }
        private void RefreshVisibility()
        {
            if(grid==null || grid.ItemsSource==null || resultView==null)return;
            var view=CollectionViewSource.GetDefaultView(grid.ItemsSource);
            if(view==null)return;
            view.Filter=item=>{
                var row=item as CatalogViewRow;
                if(row==null)return false;
                bool state;
                switch(resultView.SelectedIndex)
                {
                    case 0: state=!row.IsCompleted && !row.IsIgnored && row.Status=="Strong candidate";break;
                    case 1: state=row.IsCompleted;break;
                    case 2: state=!row.IsCompleted && !row.IsIgnored && row.Status!="Strong candidate";break;
                    case 3: state=row.IsIgnored;break;
                    default: state=true;break;
                }
                if(!state)return false;
                if(platformFilter!=null && platformFilter.SelectedIndex>0 &&
                    !String.Equals(row.Platform,platformFilter.SelectedItem as string,StringComparison.OrdinalIgnoreCase))return false;
                if(catalogSearch!=null && !String.IsNullOrWhiteSpace(catalogSearch.Text))
                {
                    var query=catalogSearch.Text.Trim();
                    var hay=(row.CurrentName??"")+" "+(row.Candidate??"")+" "+(row.Platform??"")+" "+(row.IgdbId??"");
                    if(hay.IndexOf(query,StringComparison.OrdinalIgnoreCase)<0)return false;
                }
                return true;
            };
            view.Refresh();
            UpdateTabs();
            // Show a useful preview immediately after scans or tab changes; do
            // not disturb an existing visible selection or marked-game state.
            if(grid.SelectedItem==null || !view.Contains(grid.SelectedItem))
            {
                var first=view.Cast<object>().OfType<CatalogViewRow>().FirstOrDefault();
                grid.SelectedItem=first;
            }
            UpdatePreview();
            UpdateSelectionActions();
        }
        private void MarkVisibleEligible()
        {
            var view=CollectionViewSource.GetDefaultView(grid.ItemsSource);
            if(view==null)return;
            foreach(var row in view.OfType<CatalogViewRow>())
                if(!row.IsIgnored && EnrichmentPlan.BatchEligible(row.Status,row.IsCompleted,row.IgdbId))
                    row.Marked=true;
            grid.Items.Refresh();UpdateSelectionActions();
        }
        private void ToggleIgnored()
        {
            if(busy || grid==null)return;
            var row=grid.SelectedItem as CatalogViewRow;
            if(row==null || row.IsCompleted)
            {
                api.Dialogs.ShowMessage("Select one unresolved catalog row to ignore or restore.","Smart Metadata Matcher");return;
            }
            bool ignore=!row.IsIgnored;
            var newIds=new HashSet<Guid>(ignoredIds);
            if(ignore)newIds.Add(row.GameId);else newIds.Remove(row.GameId);
            try {ignoredStore.Save(newIds);}
            catch(Exception ex){api.Dialogs.ShowErrorMessage(ex.Message,"Ignored list not saved");return;}
            ignoredIds=newIds;
            row.IsIgnored=ignore;row.Marked=false;
            if(ignore){row.PreviousStatus=row.Status;row.Status="Ignored";}
            else
            {
                row.Status=String.IsNullOrWhiteSpace(row.PreviousStatus)?"Unmatched":row.PreviousStatus;
                row.Evidence="Restored; reopen catalog to include in the next full scan if needed.";
            }
            grid.Items.Refresh();RefreshVisibility();
        }
        private void SetPreviewImage(Image image,string imageId,string size)
        {
            if(image==null)return;
            image.Source=null;
            if(!MetadataPlan.SafeImageId(imageId))return;
            try
            {
                var uri=new Uri("https://images.igdb.com/igdb/image/upload/"+size+"/"+imageId+".jpg");
                var bitmap=new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource=uri;
                bitmap.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnDemand;
                bitmap.EndInit();
                image.Source=bitmap;
            }
            catch {image.Source=null;}
        }
        private static string CatalogNames(IEnumerable<string> names)
        {
            var valid=(names??Enumerable.Empty<string>()).Where(x=>!String.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return valid.Count==0?"Unavailable from IGDB":String.Join(", ",valid);
        }
        private static string ExistingNames(IEnumerable<string> names)
        {
            var valid=(names??Enumerable.Empty<string>()).Where(x=>!String.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return valid.Count==0?"(empty)":String.Join(", ",valid);
        }
        private void AddCatalogField(string label,string current,List<string> available,bool inspected)
        {
            if(!inspected)
            {
                AddPreviewField(label,current,"Click Inspect IGDB fields to load", "Not inspected");
                return;
            }
            var value=CatalogNames(available);
            AddPreviewField(label,current,value,
                current!="(empty)"?"Preserve existing":value=="Unavailable from IGDB"?"Unavailable":"Fill missing");
        }
        private void InspectIgdbFields()
        {
            if (busy || inspectingFields || grid==null) return;
            var row=grid.SelectedItem as CatalogViewRow;
            long id;
            if(row==null || !Int64.TryParse(row.IgdbId,out id) || id<=0 || row.IsIgnored)
            {
                api.Dialogs.ShowMessage("Select a valid IGDB catalog result first.","Smart Metadata Matcher");
                return;
            }
            if(inspectedCatalog.ContainsKey(id)) { UpdatePreview(); return; }
            if(String.IsNullOrWhiteSpace(clientId.Text) || String.IsNullOrWhiteSpace(secret.Password))
            {
                api.Dialogs.ShowMessage("Enter IGDB credentials first.","Smart Metadata Matcher");
                return;
            }
            inspectingFields=true;
            if(inspectFieldsButton!=null)inspectFieldsButton.IsEnabled=false;
            var key=row.GameId;
            var credentialId=clientId.Text;
            var credentialSecret=secret.Password;
            previewSummary.Text="Inspecting exact IGDB ID "+id+" for field availability (read-only)...";
            Task.Run(()=>{
                try
                {
                    CatalogGame catalog;
                    using(var client=new IgdbReadOnly(credentialId,credentialSecret))
                        catalog=client.FetchById(id,CancellationToken.None);
                    window.Dispatcher.BeginInvoke(new Action(()=>{
                        inspectedCatalog[id]=catalog;
                        // Restore candidate artwork in the preview for entries
                        // loaded from prior history, which have an IGDB ID but
                        // no cached scan artwork until this manual inspection.
                        row.CatalogSummary=catalog.Summary;
                        row.CatalogCoverImageId=catalog.CoverImageId;
                        row.CatalogScreenshotImageId=catalog.ScreenshotImageId;
                        inspectingFields=false;
                        if(inspectFieldsButton!=null)inspectFieldsButton.IsEnabled=true;
                        if(window.IsVisible && grid.SelectedItem is CatalogViewRow &&
                            ((CatalogViewRow)grid.SelectedItem).GameId==key)UpdatePreview();
                    }));
                }
                catch(Exception ex)
                {
                    var message=ex.Message;
                    window.Dispatcher.BeginInvoke(new Action(()=>{
                        inspectingFields=false;
                        if(inspectFieldsButton!=null)inspectFieldsButton.IsEnabled=true;
                        if(window.IsVisible)
                            api.Dialogs.ShowErrorMessage(message,"IGDB field inspection failed (no changes made)");
                    }));
                }
            });
        }

        // Presentation only. A matching score and a catalog image are NOT proof
        // of identity; these rows describe POTENTIAL changes, not an approved write.
        private void RefreshPreviewHighlights()
        {
            if(previewHighlights==null || previewChangeHeading==null)return;
            previewHighlights.Children.Clear();
            var changed=comparisonRows.Where(x=>x.Decision=="Fill missing" ||
                x.Decision=="Optional rename").ToList();
            previewChangeHeading.Text="Potential changes  •  "+changed.Count;
            if(changed.Count==0)
            {
                var empty=Caption("No new fields identified yet. Additional IGDB fields may require inspection.",12);
                empty.Foreground=Muted();previewHighlights.Children.Add(empty);return;
            }
            foreach(var field in changed.Take(7))
            {
                var item=new Grid{Margin=new Thickness(0,5,0,5)};
                item.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(96)});
                item.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
                var fieldName=Caption(field.Field,12);
                fieldName.FontWeight=FontWeights.SemiBold;fieldName.Foreground=Muted();
                item.Children.Add(fieldName);
                var text=Caption(ShortText(field.Current,38)+"  →  "+ShortText(field.Suggested,95),12);
                text.Foreground=Good();text.TextWrapping=TextWrapping.Wrap;
                Grid.SetColumn(text,1);item.Children.Add(text);
                previewHighlights.Children.Add(item);
            }
            if(changed.Count>7)
            {
                var extra=Caption("+"+(changed.Count-7)+" more fields in the detailed comparison",11);
                extra.Foreground=Muted();previewHighlights.Children.Add(extra);
            }
            var caution=Caption("Proposed only — identity and exact-field checks are required before import.",11);
            caution.Foreground=Caution();caution.Margin=new Thickness(0,5,0,0);
            previewHighlights.Children.Add(caution);
        }
        private void UpdatePreview()
        {
            if(previewDetails==null||previewSummary==null||previewTitle==null)return;
            var row=grid==null?null:grid.SelectedItem as CatalogViewRow;
            comparisonRows.Clear();
            if(row==null)
            {
                previewTitle.Text="Metadata preview";
                previewDetails.Text="Select a game to compare current and suggested metadata.";
                previewSummary.Text="No writes occur during scanning. Confirm the catalog identity before applying.";
                if(previewState!=null)previewState.Text="Select a catalog result to inspect.";
                if(previewCover!=null)previewCover.Source=null;
                if(previewIcon!=null)previewIcon.Source=null;
                if(previewBackground!=null)previewBackground.Source=null;
                if(previewComparison!=null)previewComparison.Items.Refresh();
                RefreshPreviewHighlights();
                if(previewDiagnostics!=null)previewDiagnostics.Text="No catalog selection.";
                return;
            }
            previewTitle.Text="Metadata preview — "+row.CurrentName;
            var game=api.Database.Games[row.GameId];
            var current=game==null?row.CurrentName:game.Name;
            bool rename=useCatalogTitle!=null&&useCatalogTitle.IsChecked==true&&
                !String.IsNullOrWhiteSpace(row.Candidate)&&row.Candidate!="(none)"&&row.Candidate!="(ignored)";
            var libraryNames=api.Database.Games.Where(g=>g!=null).Select(g=>
                Tuple.Create(g.Id,g.Name,g.Platforms==null?"":
                    String.Join(", ",g.Platforms.Select(p=>p.Name)))).ToList();
            var plannedName=rename?EnrichmentPlan.SuggestedUniqueDisplayTitle(
                current,row.Candidate,row.GameId,row.Platform,libraryNames):"";
            bool collision=rename && String.IsNullOrWhiteSpace(plannedName);
            var chosenName=rename&&!collision?plannedName:current;
            bool haveCover=game!=null&&!MetadataPlan.Missing(game.CoverImage);
            bool haveIcon=game!=null&&!MetadataPlan.Missing(game.Icon);
            bool haveBackground=game!=null&&!MetadataPlan.Missing(game.BackgroundImage);
            bool haveDescription=game!=null&&!MetadataPlan.Missing(game.Description);
            bool sourceCover=MetadataPlan.SafeImageId(row.CatalogCoverImageId);
            bool sourceBackground=MetadataPlan.SafeImageId(row.CatalogScreenshotImageId);
            bool catalogReady=(row.Status=="Strong candidate" || row.IsCompleted) && !row.IsIgnored;
            AddPreviewField("Name (Playnite only)",current,chosenName,
                collision?"Needs manual title review":!rename?"Title cleanup disabled":
                    current==chosenName?"No change":"Included with approved import");
            AddPreviewField("Platform",row.Platform,row.Platform,"Preserve original");
            AddPreviewField("Description",haveDescription?ShortText(game.Description,16000):"(empty)",
                haveDescription?"Keep existing":!String.IsNullOrWhiteSpace(row.CatalogSummary)?ShortText(row.CatalogSummary,16000):"Not supplied by IGDB",
                haveDescription?"Preserve":!String.IsNullOrWhiteSpace(row.CatalogSummary)?"Fill missing":"Unavailable");
            AddPreviewField("Cover",haveCover?"Already present":"(empty)",
                haveCover?"Keep existing":sourceCover?"IGDB cover available":"Not offered by catalog",
                haveCover?"Preserve":sourceCover?"Fill missing":"Unavailable");
            AddPreviewField("Icon",haveIcon?"Already present":"(empty)",
                haveIcon?"Keep existing":sourceCover?"IGDB square thumbnail":"No cover for crop",
                haveIcon?"Preserve":sourceCover?"Fill missing":"Unavailable");
            AddPreviewField("Background",haveBackground?"Already present":"(empty)",
                haveBackground?"Keep existing":sourceBackground?"IGDB screenshot available":"No screenshot offered",
                haveBackground?"Preserve":sourceBackground?"Fill missing":"Unavailable");
            var currentRelease=game!=null && game.ReleaseDate.HasValue?
                game.ReleaseDate.Value.Serialize():"(empty)";
            var currentGenres=game==null?"(empty)":ExistingNames(api.Database.Genres
                .Where(x=>game.GenreIds!=null&&game.GenreIds.Contains(x.Id)).Select(x=>x.Name));
            var currentDevelopers=game==null?"(empty)":ExistingNames(api.Database.Companies
                .Where(x=>game.DeveloperIds!=null&&game.DeveloperIds.Contains(x.Id)).Select(x=>x.Name));
            var currentPublishers=game==null?"(empty)":ExistingNames(api.Database.Companies
                .Where(x=>game.PublisherIds!=null&&game.PublisherIds.Contains(x.Id)).Select(x=>x.Name));
            var currentSeries=game==null?"(empty)":ExistingNames(api.Database.Series
                .Where(x=>game.SeriesIds!=null&&game.SeriesIds.Contains(x.Id)).Select(x=>x.Name));
            CatalogGame inspected=null;
            long catalogId;
            bool haveDetails=Int64.TryParse(row.IgdbId,out catalogId) &&
                inspectedCatalog.TryGetValue(catalogId,out inspected);
            var releaseYear="Click Inspect IGDB fields to load platform-specific date";
            if(haveDetails)
            {
                var platformId=CatalogCore.PlatformId(row.Platform);
                var years=(inspected.ReleaseDates??new List<CatalogRelease>())
                    .Where(x=>x.PlatformId==platformId&&x.Year>0).Select(x=>x.Year).ToList();
                releaseYear=years.Count==0?"Unavailable for this platform":years.Min().ToString();
            }
            AddPreviewField("Release year",currentRelease,releaseYear,
                !haveDetails?"Not inspected":currentRelease!="(empty)"?"Preserve existing":
                    releaseYear=="Unavailable for this platform"?"Unavailable":"Fill missing");
            AddCatalogField("Genres",currentGenres,haveDetails?inspected.Genres:null,haveDetails);
            AddCatalogField("Developers",currentDevelopers,haveDetails?inspected.Developers:null,haveDetails);
            AddCatalogField("Publishers",currentPublishers,haveDetails?inspected.Publishers:null,haveDetails);
            AddCatalogField("Series",currentSeries,haveDetails?inspected.SeriesNames:null,haveDetails);
            if(previewComparison!=null)previewComparison.Items.Refresh();
            previewDetails.Text="IGDB: "+(row.Candidate??"—")+"  •  "+row.Platform;
            if(previewState!=null)
            {
                previewState.Text=row.IsCompleted?"Completed — you may enrich newly supported missing fields":row.IsIgnored?"Ignored":
                    catalogReady?"Strong candidate — still requires identity confirmation":"Needs manual review";
                previewState.Foreground=row.IsCompleted?Good():catalogReady?Accent():Caution();
            }
            int proposed=0;
            if(rename&&!collision&&current!=chosenName)proposed++;
            if(!haveDescription&&!String.IsNullOrWhiteSpace(row.CatalogSummary))proposed++;
            if(!haveCover&&sourceCover)proposed++;
            if(!haveIcon&&sourceCover)proposed++;
            if(!haveBackground&&sourceBackground)proposed++;
            previewSummary.Text=proposed+" possible updates from the scanned fields. " +
                (collision?"Title needs a separate manual decision. ":"")+
                "Only Playnite display names change; ROM filenames and launch paths remain untouched. " +
                "Review the identity before applying.";
            if(previewDiagnostics!=null)
                previewDiagnostics.Text="IGDB ID: "+(row.IgdbId??"—")+"  •  Match score: "+
                    (row.MatchScore??"—")+" (heuristic, not a probability)\n"+
                    (row.Evidence??"")+"\n"+
                    (haveDetails?"Additional fields inspected for this exact ID.":
                    "Inspect additional IGDB fields to see optional metadata.");
            RefreshPreviewHighlights();
            SetPreviewImage(previewCover,row.CatalogCoverImageId,"t_cover_big");
            SetPreviewImage(previewIcon,row.CatalogCoverImageId,"t_thumb");
            SetPreviewImage(previewBackground,row.CatalogScreenshotImageId,"t_screenshot_big");
        }
        private void UpdateRowAfterUndo(Guid gameId)
        {
            var game=api.Database.Games[gameId];
            var previous=enrichmentJournal.Committed().FirstOrDefault(e=>e.GameId==gameId && game!=null &&
                EnrichmentPlan.IsFullyApplied(e,game.Name,game.SortingName,game.Description,
                    game.CoverImage,game.Icon,game.BackgroundImage) &&
                EnrichmentPlan.ExtraFieldsApplied(e,GameExtrasSnapshot.ReleaseValue(game),
                    game.GenreIds,game.DeveloperIds,game.PublisherIds,game.SeriesIds));
            foreach(var row in results.Where(x=>x.GameId==gameId))
            {
                if(game!=null)row.CurrentName=game.Name;
                row.IsCompleted=previous!=null;
                row.Marked=false;
                row.Status=previous!=null?"Completed":row.FromHistory?"Review":
                    (String.IsNullOrEmpty(row.PreviousStatus)?"Review":row.PreviousStatus);
            }
            grid.Items.Refresh();
            RefreshHistory();RefreshVisibility();
        }
        private static bool CanSelectForImport(CatalogViewRow row)
        {
            return row!=null && !row.IsCompleted && !row.IsIgnored &&
                row.Status=="Strong candidate";
        }
        private List<CatalogViewRow> VisibleReadyRows()
        {
            if(grid==null || grid.ItemsSource==null)return new List<CatalogViewRow>();
            var view=CollectionViewSource.GetDefaultView(grid.ItemsSource);
            return view==null?new List<CatalogViewRow>():
                view.OfType<CatalogViewRow>().Where(CanSelectForImport).ToList();
        }
        private void UpdateSelectionActions()
        {
            if(grid==null || selectionToggleButton==null)return;
            var highlightedRows=grid.SelectedItems.OfType<CatalogViewRow>().ToList();
            var highlighted=highlightedRows.Where(CanSelectForImport).ToList();
            var visible=VisibleReadyRows();
            bool highlightsNeedMark=highlighted.Any(x=>!x.Marked);
            bool visibleNeedMark=visible.Any(x=>!x.Marked);
            selectionToggleButton.Content=highlighted.Count==0?"Highlight games first":
                highlightsNeedMark?"Select highlighted ("+highlighted.Count+")":
                "Deselect highlighted ("+highlighted.Count+")";
            selectionToggleButton.IsEnabled=!busy&&highlighted.Count>0;
            if(visibleToggleButton!=null){
                visibleToggleButton.Content=visible.Count==0?"No ready games in view":
                    visibleNeedMark?"Select visible ready ("+visible.Count+")":
                    "Deselect visible ready ("+visible.Count+")";
                visibleToggleButton.IsEnabled=!busy&&visible.Count>0;
            }
            int selected=results.Count(x=>x.Marked && CanSelectForImport(x));
            if(clearImportSelectionButton!=null){
                clearImportSelectionButton.Visibility=selected>0?Visibility.Visible:Visibility.Collapsed;
                clearImportSelectionButton.IsEnabled=!busy&&selected>0;
            }
            if(ignoreHighlightedButton!=null){
                var active=highlightedRows.FirstOrDefault();
                ignoreHighlightedButton.Content=active!=null && active.IsIgnored?
                    "Restore highlighted":"Ignore highlighted";
                ignoreHighlightedButton.IsEnabled=!busy && active!=null && !active.IsCompleted;
            }
            if(reviewSelectionButton!=null){
                reviewSelectionButton.Content=selected>0?
                    "Review & apply ("+selected+")":"Review matches";
                reviewSelectionButton.IsEnabled=!busy&&results.Count>0;
            }
        }
        private void ToggleHighlightedSelection()
        {
            if(grid==null||busy)return;
            var scope=grid.SelectedItems.OfType<CatalogViewRow>()
                .Where(CanSelectForImport).ToList();
            if(scope.Count==0)return;
            bool mark=scope.Any(x=>!x.Marked);
            foreach(var row in scope)row.Marked=mark;
            grid.Items.Refresh();UpdateSelectionActions();
        }
        private void ToggleVisibleReadySelection()
        {
            if(busy)return;
            var scope=VisibleReadyRows();
            if(scope.Count==0)return;
            bool mark=scope.Any(x=>!x.Marked);
            foreach(var row in scope)row.Marked=mark;
            grid.Items.Refresh();UpdateSelectionActions();
        }
        private void MarkHighlighted(bool selected)
        {
            if(grid==null)return;
            foreach(var row in grid.SelectedItems.OfType<CatalogViewRow>())
            {
                if(!row.IsCompleted && !row.IsIgnored && row.Status=="Strong candidate") row.Marked=selected;
            }
            grid.Items.Refresh();UpdateSelectionActions();
        }
        private void Column(string label, string property, double width)
        {
            grid.Columns.Add(new DataGridTextColumn { Header=label, Binding=new Binding(property), Width=width, IsReadOnly=true });
        }
        private void ShowTools(Button source)
        {
            if (busy)
            {
                api.Dialogs.ShowMessage("Finish or cancel the current catalog operation first.",
                    "Smart Metadata Matcher");
                return;
            }
            var menu = new ContextMenu();
            var diagnostics = new MenuItem { Header = "Export full library metadata diagnostics CSV..." };
            diagnostics.Click += (s,e) => {
                try { DiagnosticExport.Run(api); }
                catch(Exception ex) { api.Dialogs.ShowErrorMessage(ex.Message,"Metadata diagnostics export"); }
            };
            menu.Items.Add(diagnostics);
            var titles = new MenuItem { Header = "Open dedicated title cleanup / rename window..." };
            titles.ToolTip = "Closes the dashboard and opens the existing title-review workflow; scans do not persist between dashboard openings.";
            titles.Click += (s,e) => {
                try
                {
                    // Avoid competing Playnite-modal windows. All completed changes
                    // are already journaled; any un-applied catalog scan is discarded.
                    window.Close();
                    new PreviewWindow(api,pluginDataPath).Show();
                }
                catch(Exception ex) { api.Dialogs.ShowErrorMessage(ex.Message,"Title review could not open"); }
            };
            menu.Items.Add(titles);
            menu.Items.Add(new Separator());
            var history = new MenuItem { Header = "View full metadata-import history..." };
            history.Click += (s,e) => ShowFullHistory();
            menu.Items.Add(history);
            var columns = new MenuItem { Header = "Show technical matching columns", IsCheckable = true,
                IsChecked = diagnosticColumnsVisible };
            columns.Click += (s,e) => { diagnosticColumnsVisible = columns.IsChecked; UpdateDiagnosticColumns(); };
            menu.Items.Add(columns);
            menu.Items.Add(new Separator());
            var legacy = new MenuItem { Header = "Legacy supervised testing (private previews only)" };
            var one = new MenuItem { Header = "Apply selected game — legacy one-game path" };
            one.Click += (s,e) => ApplySelected(); legacy.Items.Add(one);
            var marked = new MenuItem { Header = "Apply marked games — existing five-game limit" };
            marked.Click += (s,e) => ApplyMarked(); legacy.Items.Add(marked);
            var automatic = new MenuItem { Header = "Strict auto-plan — existing 25-game limit" };
            automatic.Click += (s,e) => AutoPlan(); legacy.Items.Add(automatic);
            legacy.ToolTip="Legacy testing only. Not the approved v0.5 bulk-workflow design. Back up Playnite before any writing.";
            menu.Items.Add(legacy);
            menu.PlacementTarget = source;
            menu.IsOpen = true;
        }

        // Preserve focus on the just-completed game instead of leaving the
        // preview blank when the Active filter removes it.
        private void FocusCompleted(CatalogViewRow row)
        {
            if (row==null || grid==null || !row.IsCompleted) return;
            resultView.SelectedIndex=1;
            RefreshVisibility();
            if (!grid.Items.Contains(row))
            {
                // A narrow search can hide a renamed game. Make its result visible.
                if (catalogSearch!=null) catalogSearch.Text="";
                if (platformFilter!=null) platformFilter.SelectedIndex=0;
                RefreshVisibility();
            }
            if (grid.Items.Contains(row))
            {
                grid.SelectedItem=row;
                grid.ScrollIntoView(row);
                UpdatePreview();
            }
        }

        private void FocusAfterUndo(Guid gameId)
        {
            if (grid==null) return;
            var row=results.FirstOrDefault(r=>r.GameId==gameId);
            if (row==null) return;
            resultView.SelectedIndex=4; // All results: applies to restored history rows too.
            if (catalogSearch!=null) catalogSearch.Text="";
            if (platformFilter!=null) platformFilter.SelectedIndex=0;
            RefreshVisibility();
            if (grid.Items.Contains(row))
            {
                grid.SelectedItem=row;
                grid.ScrollIntoView(row);
                UpdatePreview();
            }
        }

        private void SaveCredentials()
        {
            try
            {
                credentialStore.Save(clientId.Text,secret.Password);
                summary.Text="Credentials encrypted for current Windows user. No network request performed yet.";
            }
            catch(Exception ex) { api.Dialogs.ShowErrorMessage(ex.Message,"Smart Metadata Matcher"); }
        }
        private void Cancel()
        {
            if (cancellation != null) cancellation.Cancel();
        }
        private void StartScan()
        {
            if (busy) return;
            if (String.IsNullOrWhiteSpace(clientId.Text) || String.IsNullOrWhiteSpace(secret.Password))
            {
                api.Dialogs.ShowMessage("Enter your Twitch developer Client ID and Client Secret locally to use IGDB.", "Smart Metadata Matcher");
                return;
            }
            if (cursor >= queue.Count) { summary.Text="All eligible games have been scanned in this session."; return; }
            var id = clientId.Text;
            var password = secret.Password;
            var count=CatalogCore.NextScanCount(queue.Count-cursor,scanSize.SelectedIndex);
            var batch = queue.Skip(cursor).Take(count).ToList();
            busy=true; scanButton.IsEnabled=false; scanSize.IsEnabled=false; cancelButton.IsEnabled=true;cancelButton.Visibility=Visibility.Visible;
            UpdateSelectionActions();
            cancellation=new CancellationTokenSource();
            var token=cancellation.Token;
            summary.Text="Searching IGDB for " + batch.Count + " games; " + cursor + " already processed. Read-only; you can cancel.";
            Task.Run(() =>
            {
                var next = new List<CatalogViewRow>();
                int done=0;
                try
                {
                    using(var client = new IgdbReadOnly(id,password))
                    {
                        foreach(var item in batch)
                        {
                            token.ThrowIfCancellationRequested();
                            var queries=CatalogCore.QueryVariants(item.Name,item.Platform);
                            var found=client.SearchVariants(queries.Select(q=>q.Text),token);
                            var match=CatalogCore.Choose(item,found);
                            var game=match.Game;
                            next.Add(new CatalogViewRow {
                                GameId=item.Id, CurrentName=item.Name, Platform=item.Platform,
                                AutoPlanEligible=CatalogCore.CanAutoPlan(match),
                                Candidate=game == null ? "(none)" : game.Name,
                                Status=match.MatchStatus, MatchScore=match.Score.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture),
                                IgdbId=game == null ? "" : game.Id.ToString(),
                                AvailableMetadata=game == null ? "" :
                                    (String.IsNullOrWhiteSpace(game.Summary)?"":"description ")+
                                    (String.IsNullOrWhiteSpace(game.CoverImageId)?"":"cover ")+
                                    (String.IsNullOrWhiteSpace(game.ScreenshotImageId)?"":"screenshot"),
                                Evidence=match.Reason,
                                CatalogSummary=game==null?null:game.Summary,
                                CatalogCoverImageId=game==null?null:game.CoverImageId,
                                CatalogScreenshotImageId=game==null?null:game.ScreenshotImageId,
                                Queries=String.Join(" | ",queries.Select(q=>q.Text)),
                                FoundCount=match.CandidatesFound.ToString(),
                                ReleaseYear=game==null || game.FirstReleaseYear==0?"":game.FirstReleaseYear.ToString(),
                                Alternatives=match.Alternatives==null?"":String.Join(" | ",match.Alternatives.Select(g=>
                                    g.Name+" ["+g.Id+"; "+String.Join("/",(g.Platforms??new List<string>()).Take(2))+"]"))
                            });
                            done++;
                            var progress=done;
                            window.Dispatcher.BeginInvoke(new Action(() => { if (window.IsVisible) summary.Text="Processed " + progress + " / " + batch.Count + " (IGDB read-only)"; }));
                            // IGDB permits 4 requests/s. Stay below the limit and leave space for retries.
                            if (done < batch.Count && token.WaitHandle.WaitOne(340)) token.ThrowIfCancellationRequested();
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    var error=ex.Message;
                    window.Dispatcher.BeginInvoke(new Action(() => { if (window.IsVisible) api.Dialogs.ShowErrorMessage(error,"IGDB catalog lookup stopped"); }));
                }
                window.Dispatcher.BeginInvoke(new Action(() => {
                    results.AddRange(next); cursor += done;
                    grid.ItemsSource=null; grid.ItemsSource=results;
                    RefreshVisibility();
                    busy=false; scanButton.IsEnabled=true; scanSize.IsEnabled=true; cancelButton.IsEnabled=false;cancelButton.Visibility=Visibility.Collapsed;
                    if (window.IsVisible)
                        summary.Text=cursor+" / "+queue.Count+" scanned  •  "+
                            results.Count(x=>x.Status=="Strong candidate")+" strong  •  "+
                            results.Count(x=>x.Status=="Review")+" review  •  "+
                            results.Count(x=>x.Status=="Unmatched")+" unmatched  •  "+
                            results.Count(x=>x.IsCompleted)+" completed.  "+
                            "No changes made.  Strong is not a calibrated probability.";
                }));
            });
        }
        private sealed class BatchItem
        {
            internal CatalogViewRow Row;
            internal string Name, Platform, Description, Cover, Icon, Background, Sorting, Candidate, PlannedTitle;
            internal GameExtrasSnapshot Extra;
            internal long IgdbId;
        }
        // Optional one-confirmation batch: no marking games one by one.
        // Nothing is written before a scrollable approval dialog is accepted.
        // Deliberately exclude same-platform title collisions and competing IGDB
        // IDs within the scanned results. More ambiguous items stay reviewable.
        private void AutoPlan()
        {
            if(busy) return;
            var library=api.Database.Games.Where(g=>g!=null).Select(g=>Tuple.Create(
                g.Id,g.Name,g.Platforms==null?"":String.Join(", ",g.Platforms.Select(p=>p.Name)))).ToList();
            var plan=EnrichmentJobPlanner.Create(results.Select(x=>new EnrichmentCandidate {
                GameId=x.GameId,CurrentName=x.CurrentName,Platform=x.Platform,
                CandidateTitle=x.Candidate,CatalogId=x.IgdbId,Status=x.Status,
                StrictEvidence=x.AutoPlanEligible,Completed=x.IsCompleted,Ignored=x.IsIgnored,
                FromHistory=x.FromHistory,Marked=x.Marked,
                PriorFailure=(x.Evidence??"").StartsWith("Apply failed:",StringComparison.Ordinal)
            }),library,useCatalogTitle.IsChecked==true,false);
            var ready=new HashSet<Guid>(plan.Where(x=>x.ReadyForPreflight).Select(x=>x.GameId));
            var safe=results.Where(x=>ready.Contains(x.GameId))
                .OrderBy(x=>x.CurrentName,StringComparer.OrdinalIgnoreCase).Take(25).ToList();
            if(safe.Count==0)
            {
                api.Dialogs.ShowMessage("No strict candidates ready for preflight in this scan. " +
                    "Review uncertain identities and duplicate IGDB IDs, or scan more games. " +
                    "No metadata changes were made.","Smart Metadata Matcher");
                return;
            }
            var excluded=plan.Count(x=>!x.ReadyForPreflight && x.Code!="NotSelected");
            RunBatch(safe,true,excluded);
        }

        // Full, scrollable explicit approval. This shows ALL planned games, unlike
        // a truncated Windows message box. Requires a real click to begin writes.
        private bool ConfirmAutoPlan(List<BatchItem> items,bool rename,int excluded)
        {
            var dialog=new Window { Title="Smart Metadata Matcher - approve strict batch plan",
                Width=1000,Height=620,MinWidth=650,MinHeight=420,
                Owner=window,WindowStartupLocation=WindowStartupLocation.CenterOwner,
                Background=Deep(),ResizeMode=ResizeMode.CanResize };
            var body=new DockPanel { Margin=new Thickness(18) };
            var head=new TextBlock {
                Text="Review " + items.Count + " proposed IGDB matches before any Playnite changes. " +
                    "This is NOT a probability guarantee. Correct title, platform and edition are your responsibility.",
                TextWrapping=TextWrapping.Wrap,FontSize=15,Foreground=Muted(),Margin=new Thickness(0,0,0,12) };
            DockPanel.SetDock(head,Dock.Top);body.Children.Add(head);
            var footer=new StackPanel {Orientation=Orientation.Horizontal,
                HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};
            var approve=new Button {Content="Approve and apply " + items.Count + " games",
                Padding=new Thickness(16,9,16,9),Margin=new Thickness(0,0,12,0),
                Background=Accent(),Foreground=Deep(),FontWeight=FontWeights.SemiBold};
            var cancel=new Button {Content="Cancel - keep library unchanged",
                Padding=new Thickness(16,9,16,9),Background=PanelBrush(),Foreground=Muted()};
            approve.Click+=(s,e)=>{dialog.DialogResult=true;};
            cancel.Click+=(s,e)=>{dialog.DialogResult=false;};
            footer.Children.Add(approve);footer.Children.Add(cancel);
            DockPanel.SetDock(footer,Dock.Bottom);body.Children.Add(footer);
            var header="STRICT AUTO-PLAN (one approval, up to 25 games)\n"+
                "Only empty fields are filled. ROMs and launch settings are untouched.\n"+
                "Playnite display names are cleaned during import; same-platform duplicates get a platform tag.\n"+
                "Excluded or review-required in this scan: " + excluded + " record(s).\n"+
                "Each success gets its own persistent undo record. Cancel stops future games, not completed ones.\n\n";
            var lines=items.Select((x,i)=>(i+1).ToString("00") + ". " +x.Name+
                "  ->  " +x.PlannedTitle+"  [IGDB: "+x.Candidate+" / "+x.IgdbId+"; "+x.Platform+"]");
            var preview=new TextBox {Text=header+String.Join("\n",lines)+
                "\n\nWARNING: A wrong catalog identity would import incorrect information. " +
                "Only approve after examining this entire list.",IsReadOnly=true,
                FontFamily=new FontFamily("Consolas"),FontSize=13,TextWrapping=TextWrapping.Wrap,
                VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,
                Background=PanelBrush(),Foreground=Muted(),
                BorderBrush=StrokeBrush(),Padding=new Thickness(12)};
            body.Children.Add(preview);dialog.Content=body;
            return dialog.ShowDialog()==true;
        }

        // Retain the five-game manually marked path for ambiguous or special cases.
        private void ApplyMarked()
        {
            if(busy) return;
            var marked=results.Where(x=>x.Marked).ToList();
            if(marked.Count==0)
            {
                api.Dialogs.ShowMessage("First tick a few Strong catalog candidates. Highlighting alone does not mark them.",
                    "Smart Metadata Matcher");return;
            }
            if(marked.Count>5 || marked.Any(x=>x.IsIgnored || !EnrichmentPlan.BatchEligible(x.Status,x.IsCompleted,x.IgdbId)))
            {
                api.Dialogs.ShowMessage("Choose 1–5 uncompleted Strong candidates only. Clear other marks before applying.",
                    "Smart Metadata Matcher");return;
            }
            RunBatch(marked,false,0);
        }

        private bool JournalAllowsNewWrites()
        {
            try
            {
                if(!EnrichmentPlan.HasUnresolvedJournalEntry(enrichmentJournal.OpenEntries())) return true;
                api.Dialogs.ShowErrorMessage("An interrupted metadata operation exists. " +
                    "Resolve it through history/undo or restore from your verified Playnite backup " +
                    "before starting another import. No new writes were started.",
                    "Recovery required before enrichment");
            }
            catch(Exception ex)
            {
                api.Dialogs.ShowErrorMessage("Enrichment journal cannot be verified: "+ex.Message+
                    "\nNo new writes were started.","Recovery required before enrichment");
            }
            return false;
        }
        private void RunBatch(List<CatalogViewRow> marked,bool automatic,int excluded)
        {
            if(!JournalAllowsNewWrites()) return;
            if(busy || marked==null || marked.Count==0 || marked.Count>(automatic?25:5)) return;
            if(automatic && marked.Any(x=>!x.AutoPlanEligible || x.IsIgnored || x.IsCompleted || x.FromHistory))
            { api.Dialogs.ShowMessage("Auto-plan gate changed. Rescan first.","Smart Metadata Matcher");return; }
            if(String.IsNullOrWhiteSpace(clientId.Text)||String.IsNullOrWhiteSpace(secret.Password))
            {
                api.Dialogs.ShowMessage("Enter IGDB credentials first.","Smart Metadata Matcher");return;
            }
            var items=new List<BatchItem>();
            foreach(var row in marked)
            {
                long id;
                if(!Int64.TryParse(row.IgdbId,out id)||id<=0)
                { api.Dialogs.ShowMessage("A marked IGDB ID is invalid; rescan.","Smart Metadata Matcher");return; }
                var game=api.Database.Games[row.GameId];
                if(game==null || !String.Equals(game.Name,row.CurrentName,StringComparison.Ordinal) ||
                    !String.Equals(row.Platform,game.Platforms==null?"":String.Join(", ",game.Platforms.Select(p=>p.Name)),StringComparison.Ordinal))
                { api.Dialogs.ShowMessage("A marked game changed since scanning. Rescan before applying.","Smart Metadata Matcher");return; }
                items.Add(new BatchItem {Row=row,Name=game.Name,Platform=row.Platform,
                    Description=game.Description,Cover=game.CoverImage,Icon=game.Icon,
                    Background=game.BackgroundImage,Sorting=game.SortingName,IgdbId=id,
                    Extra=GameExtrasSnapshot.Capture(game),Candidate=row.Candidate});
            }
            bool rename=useCatalogTitle.IsChecked==true;
            var plannedLibrary=api.Database.Games.Where(g=>g!=null).Select(g=>
                Tuple.Create(g.Id,g.Name,g.Platforms==null?"":
                    String.Join(", ",g.Platforms.Select(p=>p.Name)))).ToList();
            var proposed=new List<string>();
            foreach(var item in items)
            {
                var wanted=rename?EnrichmentPlan.SuggestedUniqueDisplayTitle(
                    item.Name,item.Candidate,item.Row.GameId,item.Platform,plannedLibrary):"";
                item.PlannedTitle=String.IsNullOrWhiteSpace(wanted)?item.Name:wanted;
                // Reserve the proposed name so subsequent marked games cannot
                // accidentally receive a duplicate name in the SAME approved plan.
                var pos=plannedLibrary.FindIndex(x=>x.Item1==item.Row.GameId);
                if(pos>=0)plannedLibrary[pos]=Tuple.Create(item.Row.GameId,item.PlannedTitle,item.Platform);
                proposed.Add(item.Name+" → "+item.PlannedTitle+" [IGDB "+item.IgdbId+"]"+
                    (rename && !EnrichmentPlan.IsSafeTitle(wanted)?
                        " (title requires manual review; other metadata can proceed)":""));
            }
            var question="Import missing metadata for "+items.Count+" marked games?\n\n"+
                String.Join("\n",proposed)+"\n\n"+
                "Only missing fields will be filled: description, artwork, release year, genres, developers, " +
                "publishers and series when provided by IGDB. The proposed Playnite display titles " +
                "are included automatically in this approved import; same-platform duplicates get a platform tag. " +
                "No game files, launch settings or existing media will be overwritten. " +
                "Every successful game is independently journaled and can be undone in reverse order.";
            if(automatic)
            {
                if(!ConfirmAutoPlan(items,rename,excluded))return;
            }
            else if(api.Dialogs.ShowMessage(question,"Confirm supervised batch enrichment (up to 5)",
                MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
            if(!JournalAllowsNewWrites()) return;
            var credentialId=clientId.Text;var credentialSecret=secret.Password;
            busy=true;scanButton.IsEnabled=false;applyButton.IsEnabled=false;batchButton.IsEnabled=false;
            autoBatchButton.IsEnabled=false;
            undoButton.IsEnabled=false;scanSize.IsEnabled=false;cancelButton.IsEnabled=true;cancelButton.Visibility=Visibility.Visible;
            cancellation=new CancellationTokenSource();var token=cancellation.Token;
            Task.Run(()=>{
                int completed=0,failed=0;var errors=new List<string>();
                bool cancelled=false,recoveryRequired=false;
                CatalogViewRow lastApplied=null;
                try
                {
                    using(var client=new IgdbReadOnly(credentialId,credentialSecret))
                    foreach(var item in items)
                    {
                        if(token.IsCancellationRequested){cancelled=true;break;}
                        try
                        {
                            var cat=client.FetchById(item.IgdbId,token);
                            if(!String.Equals(cat.Name,item.Candidate,StringComparison.Ordinal))
                                throw new InvalidOperationException("IGDB title changed; rescan required.");
                            if(automatic)
                            {
                                // Verify independent title/platform evidence again from
                                // exact catalog ID, before downloading images or writing.
                                var rechecked=CatalogCore.Choose(new CatalogInput {
                                    Name=item.Name,Platform=item.Platform},new[]{cat});
                                if(!CatalogCore.CanAutoPlan(rechecked))
                                    throw new InvalidOperationException("Strict identity evidence failed the live IGDB recheck; manual review required.");
                            }
                            byte[] cover=null,icon=null,background=null;
                            if(MetadataPlan.Missing(item.Cover)&&MetadataPlan.SafeImageId(cat.CoverImageId))
                                cover=client.DownloadCover(cat.CoverImageId,token);
                            if(MetadataPlan.Missing(item.Icon)&&MetadataPlan.SafeImageId(cat.CoverImageId))
                            {
                                try {icon=client.DownloadIcon(cat.CoverImageId,token);}
                                catch(OperationCanceledException){throw;}
                                catch(Exception){ /* Optional icon absent; other fields may still apply. */ }
                            }
                            if(MetadataPlan.Missing(item.Background)&&MetadataPlan.SafeImageId(cat.ScreenshotImageId))
                            {
                                try {background=client.DownloadBackground(cat.ScreenshotImageId,token);}
                                catch(OperationCanceledException){throw;}
                                catch(Exception){ /* Optional screenshot absent; other fields may still apply. */ }
                            }
                            token.ThrowIfCancellationRequested();
                            // SDK database and WPF state only on Playnite's UI thread.
                            window.Dispatcher.Invoke(new Action(()=>{
                                if(token.IsCancellationRequested || !window.IsVisible)return;
                                CatalogEnrichmentWriter.ApplyOne(api,enrichmentJournal,item.Row.GameId,
                                    item.Name,item.Platform,item.Description,item.Cover,item.Icon,
                                    item.Background,item.Sorting,cat,cover,icon,background,rename,item.Extra,
                                    item.PlannedTitle);
                                item.Row.PreviousStatus=item.Row.Status;
                                item.Row.IsCompleted=true;item.Row.Marked=false;item.Row.Status="Completed";
                                item.Row.CurrentName=api.Database.Games[item.Row.GameId].Name;
                                lastApplied=item.Row;
                                completed++;
                                grid.Items.Refresh();RefreshHistory();RefreshVisibility();
                                summary.Text=(automatic?"Strict batch":"Supervised batch")+
                                    " progress: "+(completed+failed)+" / "+items.Count+
                                    "; "+completed+" applied; "+failed+" failed.";
                            }));
                        }
                        catch(OperationCanceledException){cancelled=true;break;}
                        catch(Exception ex)
                        {
                            failed++;errors.Add(item.Name+": "+ex.Message);
                            // An exception after durable Prepare can mean the database
                            // was partially written. Do not start the next game in that case.
                            try { recoveryRequired=EnrichmentPlan.HasUnresolvedJournalEntry(enrichmentJournal.OpenEntries()); }
                            catch(Exception journalError)
                            {
                                recoveryRequired=true;
                                errors.Add("Journal unreadable: "+journalError.Message);
                            }
                            window.Dispatcher.Invoke(new Action(()=>{
                                item.Row.Marked=false;
                                item.Row.Evidence="Apply failed: "+ex.Message;
                                item.Row.AutoPlanEligible=false;
                                grid.Items.Refresh();
                            }));
                            if(recoveryRequired)
                            {
                                errors.Add("STOPPED: unresolved journal entry; recover before more writes.");
                                break;
                            }
                        }
                        if(token.WaitHandle.WaitOne(350)){cancelled=true;break;}
                    }
                }
                catch(Exception ex){failed++;errors.Add("Batch stopped: "+ex.Message);}
                window.Dispatcher.BeginInvoke(new Action(()=>{
                    if(!window.IsVisible)return;
                    EndApply();
                    if(lastApplied!=null)FocusCompleted(lastApplied);
                    var report="Applied "+completed+" of "+items.Count+" confirmed games; "+failed+
                        " failures"+(recoveryRequired?"; RECOVERY REQUIRED before further writes":
                            cancelled?"; cancelled before remaining items":"")+
                        ". Confirmed changes have individual undo records.";
                    if(errors.Count>0)report+="\n"+String.Join("\n",errors.Take(5));
                    summary.Text=report;api.Dialogs.ShowMessage(report,"Smart Metadata Matcher");
                }));
            });
        }
        private void ApplySelected()
        {
            if(busy || !JournalAllowsNewWrites()) return;
            if(grid.SelectedItems.Count!=1)
            {
                api.Dialogs.ShowMessage("Select exactly one Strong catalog candidate to test the full import.",
                    "Smart Metadata Matcher"); return;
            }
            var row=grid.SelectedItem as CatalogViewRow;
            long igdbId;
            if(row==null || row.IsIgnored || (row.Status!="Strong candidate" && !row.IsCompleted) ||
               !Int64.TryParse(row.IgdbId,out igdbId) || igdbId<=0 ||
               (row.IsCompleted && !enrichmentJournal.Committed().Any(e=>
                   e.GameId==row.GameId && e.IgdbId==igdbId)))
            {
                api.Dialogs.ShowMessage("Choose a Strong candidate or a previously completed game, and confirm its identity. " +
                    "The score alone is not proof.","Smart Metadata Matcher"); return;
            }
            var game=api.Database.Games[row.GameId];
            if(game==null || !String.Equals(game.Name,row.CurrentName,StringComparison.Ordinal))
            {
                api.Dialogs.ShowMessage("This game changed since the catalog scan. Reopen and rescan.",
                    "Smart Metadata Matcher"); return;
            }
            var platform=game.Platforms==null?"":String.Join(", ",game.Platforms.Select(p=>p.Name));
            if(!String.Equals(platform,row.Platform,StringComparison.Ordinal))
            {
                api.Dialogs.ShowMessage("The game platform changed. Rescan.","Smart Metadata Matcher"); return;
            }
            if(String.IsNullOrWhiteSpace(clientId.Text) || String.IsNullOrWhiteSpace(secret.Password))
            {
                api.Dialogs.ShowMessage("Enter your local IGDB credentials first.","Smart Metadata Matcher"); return;
            }
            var nameRequested=useCatalogTitle.IsChecked==true;
            var libraryNames=api.Database.Games.Where(g=>g!=null).Select(g=>
                Tuple.Create(g.Id,g.Name,g.Platforms==null?"":
                    String.Join(", ",g.Platforms.Select(p=>p.Name)))).ToList();
            var wanted=nameRequested?EnrichmentPlan.SuggestedUniqueDisplayTitle(
                game.Name,row.Candidate,game.Id,platform,libraryNames):"";
            var approvedTitle=String.IsNullOrWhiteSpace(wanted)?game.Name:wanted;
            var preview=(row.IsCompleted?"Enrich missing fields on a previously completed game?\n\n":
                "Apply IGDB metadata to exactly ONE game?\n\n")+
                "Current title: "+game.Name+"\nCatalog identity: "+row.Candidate+
                " (IGDB "+row.IgdbId+", year "+row.ReleaseYear+")\nPlatform: "+platform+"\n\n"+
                "If missing: description, cover, icon (IGDB square thumbnail), background (IGDB screenshot), " +
                "genres, developers, publishers, series and platform-specific release year.\n"+
                (nameRequested ? (String.IsNullOrWhiteSpace(wanted)?
                    "Name requires manual review; other metadata can still be imported.\n" :
                    "Playnite name after import: "+approvedTitle+" (ROMs and launch paths unchanged).\n") :
                    "Playnite title cleanup disabled for this operation.\n")+
                "\nExisting metadata, custom sorting, game files and launch settings remain unchanged. " +
                "Review the identity and edition. Undo records all fields changed.";
            if(api.Dialogs.ShowMessage(preview,"Confirm ONE complete metadata import",
                MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
            var originalName=game.Name;
            var originalDescription=game.Description;
            var originalCover=game.CoverImage;
            var originalIcon=game.Icon;
            var originalBackground=game.BackgroundImage;
            var originalSorting=game.SortingName;
            var originalExtras=GameExtrasSnapshot.Capture(game);
            var credentialId=clientId.Text;
            var credentialSecret=secret.Password;
            var expectedCatalogTitle=row.Candidate;
            bool coverNeeded=MetadataPlan.Missing(originalCover);
            bool iconNeeded=MetadataPlan.Missing(originalIcon);
            bool backgroundNeeded=MetadataPlan.Missing(originalBackground);
            busy=true; scanButton.IsEnabled=false; applyButton.IsEnabled=false;
            undoButton.IsEnabled=false; scanSize.IsEnabled=false; cancelButton.IsEnabled=true;cancelButton.Visibility=Visibility.Visible;
            cancellation=new CancellationTokenSource();
            var token=cancellation.Token;
            summary.Text="Fetching exact IGDB identity and available artwork; no library changes yet...";
            Task.Run(()=>{
                try
                {
                    CatalogGame catalog;
                    byte[] cover=null,icon=null,background=null;
                    var notices=new List<string>();
                    using(var client=new IgdbReadOnly(credentialId,credentialSecret))
                    {
                        catalog=client.FetchById(igdbId,token);
                        if(!String.Equals(catalog.Name,expectedCatalogTitle,StringComparison.Ordinal))
                            throw new InvalidOperationException("IGDB title changed since scan. Rescan before applying.");
                        if(coverNeeded && MetadataPlan.SafeImageId(catalog.CoverImageId))
                            cover=client.DownloadCover(catalog.CoverImageId,token);
                        if(iconNeeded && MetadataPlan.SafeImageId(catalog.CoverImageId))
                        {
                            try { icon=client.DownloadIcon(catalog.CoverImageId,token); }
                            catch(OperationCanceledException) { throw; }
                            catch(Exception ex) { notices.Add("Icon unavailable: "+ex.Message); }
                        }
                        if(backgroundNeeded && MetadataPlan.SafeImageId(catalog.ScreenshotImageId))
                        {
                            try { background=client.DownloadBackground(catalog.ScreenshotImageId,token); }
                            catch(OperationCanceledException) { throw; }
                            catch(Exception ex) { notices.Add("Background unavailable: "+ex.Message); }
                        }
                    }
                    token.ThrowIfCancellationRequested();
                    window.Dispatcher.BeginInvoke(new Action(()=>{
                        try
                        {
                            if(!window.IsVisible || token.IsCancellationRequested) return;
                            var entry=CatalogEnrichmentWriter.ApplyOne(api,enrichmentJournal,row.GameId,
                                originalName,platform,originalDescription,originalCover,originalIcon,
                                originalBackground,originalSorting,catalog,cover,icon,background,nameRequested,originalExtras,
                                approvedTitle);
                            row.PreviousStatus=row.Status;
                            row.IsCompleted=true;
                            row.Marked=false;
                            row.Status="Completed";
                            row.CurrentName=api.Database.Games[row.GameId].Name;
                            grid.Items.Refresh(); RefreshHistory();FocusCompleted(row);
                            var message=CatalogEnrichmentWriter.Summary(entry);
                            if(notices.Count>0) message += "\n"+String.Join("\n",notices);
                            summary.Text=message;
                            api.Dialogs.ShowMessage(message,"Smart Metadata Matcher");
                        }
                        catch(Exception ex) { api.Dialogs.ShowErrorMessage(ex.Message,"Metadata import stopped"); }
                        finally { EndApply(); }
                    }));
                }
                catch(OperationCanceledException)
                {
                    window.Dispatcher.BeginInvoke(new Action(()=>{summary.Text="Metadata fetch cancelled. No new write started."; EndApply();}));
                }
                catch(Exception ex)
                {
                    var message=ex.Message;
                    window.Dispatcher.BeginInvoke(new Action(()=>{
                        api.Dialogs.ShowErrorMessage(message,"IGDB fetch failed; nothing was applied"); EndApply();
                    }));
                }
            });
        }
        private void EndApply()
        {
            busy=false;
            scanButton.IsEnabled=true; applyButton.IsEnabled=true; batchButton.IsEnabled=true;
            autoBatchButton.IsEnabled=true;undoButton.IsEnabled=true;
            scanSize.IsEnabled=true; cancelButton.IsEnabled=false;cancelButton.Visibility=Visibility.Collapsed;
        }
        // Hide is deliberately reversible and does NOT touch the enrichment journal.
        // Open operations still block unsafe undo even if hidden from the table.
        private void HideSelectedHistory(List<HistoryRow> selected,Action refreshDetail)
        {
            if(busy)return;
            var ids=(selected??new List<HistoryRow>()).Where(r=>r!=null && !r.Hidden)
                .Select(r=>r.OperationId).Where(id=>id!=Guid.Empty).Distinct().ToList();
            if(ids.Count==0)
            { api.Dialogs.ShowMessage("Select one or more visible history rows to remove from the list.",
                "Smart Metadata Matcher");return; }
            if(api.Dialogs.ShowMessage("Hide "+ids.Count+" operation(s) from Recent Updates and the default History view?\n\n"+
                "This does NOT delete metadata, games, ROM files or undo records. You can restore hidden entries in Full history. Hidden applied operations still count toward the undo stack.",
                "Remove from history view",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            var updated=new HashSet<Guid>(hiddenOperations);
            foreach(var id in ids)updated.Add(id);
            try{historyVisibility.Save(updated);hiddenOperations=updated;RefreshHistory();if(refreshDetail!=null)refreshDetail();}
            catch(Exception ex){api.Dialogs.ShowErrorMessage(ex.Message,"History visibility update failed");}
        }
        private void RestoreSelectedHistory(List<HistoryRow> selected,Action refreshDetail)
        {
            if(busy)return;
            var ids=(selected??new List<HistoryRow>()).Where(r=>r!=null && r.Hidden)
                .Select(r=>r.OperationId).Where(id=>id!=Guid.Empty).Distinct().ToList();
            if(ids.Count==0)
            { api.Dialogs.ShowMessage("Enable Show hidden, then select hidden entries to restore.",
                "Smart Metadata Matcher");return; }
            var updated=new HashSet<Guid>(hiddenOperations);
            foreach(var id in ids)updated.Remove(id);
            try{historyVisibility.Save(updated);hiddenOperations=updated;RefreshHistory();if(refreshDetail!=null)refreshDetail();}
            catch(Exception ex){api.Dialogs.ShowErrorMessage(ex.Message,"History visibility restore failed");}
        }
        private void RenameSelectedHistoryTitles(List<HistoryRow> selected)
        {
            if(busy)return;
            if(selected==null || selected.Count==0)
            { api.Dialogs.ShowMessage("Select one or more Applied entries in Recent Updates or Full History.",
                "Smart Metadata Matcher");return; }
            // A mixed history selection should not silently make the whole action inert.
            // Ignore Undone records, and collapse repeated operations for one game.
            var applied=selected.Where(x=>x.Result=="Applied").GroupBy(x=>x.GameId)
                .Select(g=>g.First()).ToList();
            var skipped=selected.Count-applied.Count;
            if(applied.Count==0)
            { api.Dialogs.ShowMessage("No Applied operations selected. Undone records are historical and cannot be renamed.",
                "Smart Metadata Matcher");return; }
            if(applied.Count>20)
            { api.Dialogs.ShowMessage("Select at most 20 distinct games for a supervised rename.",
                "Smart Metadata Matcher");return; }
            if(!JournalAllowsNewWrites())return;
            var committed=enrichmentJournal.Committed();
            var targets=new List<Tuple<Guid,string,long,string,string,bool>>();
            var library=api.Database.Games.Where(g=>g!=null).Select(g=>Tuple.Create(g.Id,g.Name,
                g.Platforms==null?"":String.Join(", ",g.Platforms.Select(p=>p.Name)))).ToList();
            foreach(var row in applied)
            {
                // Choose the newest open identity for the game, not an older
                // selected journal event which might have a stale AfterName.
                var previous=committed.FirstOrDefault(x=>x.GameId==row.GameId &&
                    EnrichmentPlan.IsSafeTitle(x.CatalogTitle));
                var game=api.Database.Games[row.GameId];
                if(previous==null || game==null ||
                   !EnrichmentPlan.IsExpected(game.Name,previous.AfterName))
                {
                    api.Dialogs.ShowMessage("At least one selected game changed since its latest recorded import. " +
                        "Refresh the library/history before renaming; no changes made.",
                        "Smart Metadata Matcher");return;
                }
                var platform=game.Platforms==null?"":String.Join(", ",game.Platforms.Select(p=>p.Name));
                var wanted=EnrichmentPlan.SuggestedUniqueDisplayTitle(game.Name,previous.CatalogTitle,
                    game.Id,platform,library);
                if(String.IsNullOrWhiteSpace(wanted))
                {
                    api.Dialogs.ShowMessage("Cannot assign a unique, verified title for "+game.Name+
                        " on platform "+platform+". Resolve that collision manually in Playnite; no changes made.",
                        "Smart Metadata Matcher");return;
                }
                if(String.Equals(game.Name,wanted,StringComparison.Ordinal))
                { skipped++;continue; }
                var collision=EnrichmentPlan.HasCollision(wanted,game.Id,platform,library);
                targets.Add(Tuple.Create(row.GameId,game.Name,previous.IgdbId,previous.CatalogTitle,wanted,collision));
                for(var i=0;i<library.Count;i++)if(library[i].Item1==game.Id)
                    {library[i]=Tuple.Create(game.Id,wanted,platform);break;}
            }
            if(targets.Count==0)
            { api.Dialogs.ShowMessage("All eligible selected games already have their approved display titles.",
                "Smart Metadata Matcher");return; }
            var lines=targets.Select(x=>x.Item2+" → "+x.Item5);
            var question="Update "+targets.Count+" PLAYNITE display title(s)?\n\n"+
                String.Join("\n",lines)+"\n\n"+
                (skipped>0?skipped+" selected history row(s) skipped (Undone, repeated or already correct).\n\n":"")+
                "A same-platform duplicate receives a platform suffix such as (GBA). " +
                "This only changes Playnite display names and non-custom sorting names. " +
                "ROM files, paths, emulator settings and launch actions are untouched.\n\n"+
                "Each change is journaled and undoable. Proceed?";
            if(api.Dialogs.ShowMessage(question,"Confirm Playnite display-name cleanup",
                MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
            int count=0;
            try
            {
                foreach(var x in targets)
                {
                    CatalogEnrichmentWriter.RenameDisplayTitle(api,enrichmentJournal,x.Item1,x.Item2,x.Item3,x.Item4,x.Item5,x.Item6);
                    count++;
                    var live=api.Database.Games[x.Item1];
                    foreach(var r in results.Where(r=>r.GameId==x.Item1))r.CurrentName=live.Name;
                }
                grid.Items.Refresh();RefreshHistory();RefreshVisibility();UpdatePreview();
                api.Dialogs.ShowMessage("Updated "+count+" Playnite display title(s). " +
                    (skipped>0?skipped+" ineligible/repeated selection(s) skipped. ":"")+
                    "No ROM files renamed.","Smart Metadata Matcher");
            }
            catch(Exception ex)
            {
                RefreshHistory();grid.Items.Refresh();
                api.Dialogs.ShowErrorMessage("Stopped after "+count+" title(s): "+ex.Message+
                    "\nCheck the undo journal before retrying.","Title-only update stopped");
            }
        }
        private void UndoSelectedHistory(List<HistoryRow> selected)
        {
            if(busy)return;
            if(selected==null || selected.Count==0)
            { api.Dialogs.ShowMessage("Select one or more recent operations with Ctrl-click or Shift-click.",
                "Smart Metadata Matcher");return; }
            if(selected.Any(x=>x.Result!="Applied"))
            { api.Dialogs.ShowMessage("Historical undone entries are read-only. Select applied records only.",
                "Smart Metadata Matcher");return; }
            if(!JournalAllowsNewWrites())return;
            var history=enrichmentJournal.Committed();
            if(!EnrichmentPlan.IsLatestUndoPrefix(history.Select(x=>x.OperationId),selected.Select(x=>x.OperationId)))
            {
                api.Dialogs.ShowMessage("Only the latest consecutive committed operations can be undone together. " +
                    "Select the newest row(s) without skipping one; older arbitrary selections are not currently safe for stack undo.",
                    "Smart Metadata Matcher");return;
            }
            if(api.Dialogs.ShowMessage("Undo the latest "+selected.Count+" metadata operation(s), newest first? " +
                "Newer manual changes are protected, and the process stops on any conflict.",
                "Confirm selected undo",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
            int done=0;
            try
            {
                foreach(var e in history.Take(selected.Count))
                {
                    CatalogEnrichmentWriter.UndoLast(api,enrichmentJournal);
                    UpdateRowAfterUndo(e.GameId);
                    done++;
                }
                RefreshHistory();RefreshVisibility();UpdatePreview();
                api.Dialogs.ShowMessage("Undid "+done+" operation(s).","Smart Metadata Matcher");
            }
            catch(Exception ex)
            {
                RefreshHistory();
                api.Dialogs.ShowErrorMessage("Stopped after "+done+" undo(s): "+ex.Message+
                    "\nRemaining operations were not changed.","Selected undo stopped");
            }
        }
        private void UndoLastMetadata()
        {
            if(busy) return;
            try
            {
                var latest=enrichmentJournal.LatestOpen();
                if(latest!=null)
                {
                    if(api.Dialogs.ShowMessage("Undo the last confirmed metadata import?\n\n"+
                        latest.BeforeName+" → "+latest.AfterName+"\n"+
                        "Names, descriptions, artwork and catalog reference fields are restored where changed. " +
                        "Newer manual changes are protected.","Confirm metadata undo",
                        MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
                    var msg=CatalogEnrichmentWriter.UndoLast(api,enrichmentJournal);
                    UpdateRowAfterUndo(latest.GameId);
                    FocusAfterUndo(latest.GameId);
                    RefreshHistory();
                    summary.Text=msg; api.Dialogs.ShowMessage(msg,"Smart Metadata Matcher");
                    return;
                }
                // v0.4.0 journal remains supported. No migration or overwrite.
                var legacy=metadataJournal.LatestPending();
                if(legacy==null)
                {
                    api.Dialogs.ShowMessage("No metadata imports to undo.","Smart Metadata Matcher"); return;
                }
                if(api.Dialogs.ShowMessage("Undo previous v0.4.0 description/cover import for " +
                    legacy.GameName + "?", "Confirm legacy metadata undo",
                    MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return;
                var old=CatalogMetadataWriter.UndoLast(api,metadataJournal);
                UpdateRowAfterUndo(legacy.GameId);
                FocusAfterUndo(legacy.GameId);
                RefreshHistory();
                summary.Text=old; api.Dialogs.ShowMessage(old,"Smart Metadata Matcher");
            }
            catch(Exception ex) { api.Dialogs.ShowErrorMessage(ex.Message,"Metadata undo stopped safely"); }
        }
        private void Export()
        {
            if (results.Count==0) { api.Dialogs.ShowMessage("Run a catalog scan first.","Smart Metadata Matcher"); return; }
            var save = new Microsoft.Win32.SaveFileDialog {
                FileName="SmartMetadataMatcher-IGDB-Candidates.csv",
                Filter="CSV (*.csv)|*.csv", AddExtension=true, DefaultExt=".csv"
            };
            if (save.ShowDialog(window)!=true) return;
            Func<string,string> cell=(s)=> {
                var value=(s ?? "").Replace('\r',' ').Replace('\n',' ').Replace('\t',' ');
                if (value.TrimStart().Length>0 && "=+-@".IndexOf(value.TrimStart()[0]) >= 0) value="'"+value;
                return "\""+value.Replace("\"","\"\"")+"\"";
            };
            var sb = new StringBuilder();
            sb.AppendLine("Current title,Platform,IGDB candidate,Status,Heuristic score,IGDB ID,Release year,Unique candidates,Other candidates,Queries tried,Available metadata,Reason");
            foreach(var r in results)
                sb.AppendLine(String.Join(",", new[]{r.CurrentName,r.Platform,r.Candidate,r.Status,r.MatchScore,r.IgdbId,r.ReleaseYear,r.FoundCount,r.Alternatives,r.Queries,r.AvailableMetadata,r.Evidence}.Select(cell)));
            File.WriteAllText(save.FileName,sb.ToString(),new UTF8Encoding(true));
            api.Dialogs.ShowMessage("Exported " + results.Count + " candidate rows. No game files or credentials exported.","Smart Metadata Matcher");
        }
    }
}
