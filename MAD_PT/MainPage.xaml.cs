namespace MAD_PT
{
    public partial class MainPage : ContentPage
    {
        public List<Artwork> Artworks { get; } = new();
        public string BioHeader { get; } =
            "Vincent van Gogh (1853–1890) was a Dutch Post‑Impressionist painter whose bold color and expressive brushwork transformed modern art.";
        public string BioMore { get; } =
            "His emotionally charged paintings explore themes of nature, light and human experience. Among his most celebrated works are The Starry Night, Sunflowers and Starry Night Over the Rhone.";
        public string FullBio => $"{BioHeader} {BioMore}";

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            Artworks.AddRange(new[]
            {
                new Artwork("peachtreeinblossom.jpeg", "Peach Tree in Blossom", "$950,000"),
                new Artwork("cafeterraceatnight.jpg", "Cafe Terrace at Night", "$1,920,000"),
                new Artwork("thestarrynight.jpeg", "The Starry Night", "$3,200,000"),
                new Artwork("sunflowers.jpeg", "Sunflowers", "$2,400,000"),
                new Artwork("starryrhone.jpeg", "Starry Night Over the Rhone", "$1,650,000"),
                new Artwork("drawthree.jpeg", "Wheatfield with Cypresses", "$1,150,000"),
                new Artwork("drawseven.jpeg", "The Yellow House", "$870,000"),
            });
        }
    }

    public record Artwork(string Image, string Title, string Price);
}
