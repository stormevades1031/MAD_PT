namespace MAD_FINAL.Services;

public static partial class GoogleMapsConfig
{
    public static string ApiKey { get; private set; } = "YOUR_GOOGLE_MAPS_API_KEY";

    static GoogleMapsConfig()
    {
        Configure();
    }

    static partial void Configure();
}
