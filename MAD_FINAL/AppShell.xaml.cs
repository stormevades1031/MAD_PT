namespace MAD_FINAL
{
    public partial class AppShell : Shell
    {
        public AppShell(IServiceProvider services)
        {
            InitializeComponent();

            var tabBar = new TabBar();

            tabBar.Items.Add(new ShellContent
            {
                Route = "map",
                Title = "Map",
                Content = services.GetRequiredService<MainPage>(),
            });

            tabBar.Items.Add(new ShellContent
            {
                Route = "history",
                Title = "History",
                Content = services.GetRequiredService<HistoryPage>(),
            });

            Items.Add(tabBar);
        }
    }
}
