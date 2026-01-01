namespace MAD_FINAL.Services;

public static class GoogleMapsHtml
{
    public static string Build(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.Equals(apiKey.Trim(), "YOUR_GOOGLE_MAPS_API_KEY", StringComparison.Ordinal))
        {
            return MissingKeyHtml();
        }

        apiKey = apiKey.Trim();
        var template = """
<!DOCTYPE html>
<html>
<head>
  <meta name="viewport" content="initial-scale=1,maximum-scale=1,user-scalable=no" />
  <style>
    html, body, #map { height: 100%; width: 100%; margin: 0; padding: 0; background: #0f1115; }
    .chip {
      position: absolute;
      top: 92px;
      left: 12px;
      right: 12px;
      padding: 10px 12px;
      border-radius: 12px;
      background: rgba(20, 20, 20, 0.75);
      color: #fff;
      font-family: Arial, Helvetica, sans-serif;
      font-size: 12px;
      backdrop-filter: blur(10px);
    }
  </style>
</head>
<body>
  <div id="map"></div>
  <div class="chip" id="hint">Tap the map to select a destination.</div>
  <script>
    let map;
    let currentMarker;
    let destinationMarker;
    let directionsService;
    let directionsRenderer;

    function initMap() {
      map = new google.maps.Map(document.getElementById("map"), {
        center: { lat: 0, lng: 0 },
        zoom: 2,
        disableDefaultUI: true,
        zoomControl: true,
        gestureHandling: "greedy",
        clickableIcons: false,
        mapId: "MAD_FINAL"
      });

      directionsService = new google.maps.DirectionsService();
      directionsRenderer = new google.maps.DirectionsRenderer({
        suppressMarkers: true,
        preserveViewport: true,
        polylineOptions: {
          strokeColor: "#512BD4",
          strokeOpacity: 0.9,
          strokeWeight: 6
        }
      });
      directionsRenderer.setMap(map);

      map.addListener("click", (e) => {
        const lat = e.latLng.lat();
        const lng = e.latLng.lng();
        window.location.href = `app://mapclick?lat=${lat}&lng=${lng}`;
      });
    }

    function centerMap(lat, lng, zoom) {
      if (!map) return;
      map.setCenter({ lat: lat, lng: lng });
      if (zoom) map.setZoom(zoom);
    }

    function setCurrent(lat, lng) {
      if (!map) return;
      const pos = { lat: lat, lng: lng };
      if (!currentMarker) {
        currentMarker = new google.maps.Marker({
          position: pos,
          map: map,
          title: "Current Location",
          icon: {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 7,
            fillColor: "#2ECC71",
            fillOpacity: 1,
            strokeColor: "#0f1115",
            strokeWeight: 2
          }
        });
      } else {
        currentMarker.setPosition(pos);
      }
      centerMap(lat, lng, 15);
      renderRouteIfReady();
    }

    function setDestination(lat, lng) {
      if (!map) return;
      const pos = { lat: lat, lng: lng };
      if (!destinationMarker) {
        destinationMarker = new google.maps.Marker({
          position: pos,
          map: map,
          title: "Destination",
          icon: {
            path: google.maps.SymbolPath.BACKWARD_CLOSED_ARROW,
            scale: 5,
            fillColor: "#FF5C5C",
            fillOpacity: 1,
            strokeColor: "#0f1115",
            strokeWeight: 2
          }
        });
      } else {
        destinationMarker.setPosition(pos);
      }
      renderRouteIfReady();
      document.getElementById("hint").textContent = "Destination selected. Use Start Navigation to open directions.";
    }

    function renderRouteIfReady() {
      if (!currentMarker || !destinationMarker) return;
      const origin = currentMarker.getPosition();
      const destination = destinationMarker.getPosition();
      directionsService.route({
        origin: origin,
        destination: destination,
        travelMode: google.maps.TravelMode.DRIVING
      }).then((result) => {
        directionsRenderer.setDirections(result);
      }).catch(() => {
      });
    }
  </script>
  <script src="https://maps.googleapis.com/maps/api/js?key=__API_KEY__&libraries=places&callback=initMap" async defer></script>
</body>
</html>
""";

        return template.Replace("__API_KEY__", Escape(apiKey));
    }

    private static string Escape(string input)
    {
        return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    private static string MissingKeyHtml()
    {
        return """
<!DOCTYPE html>
<html>
<head>
  <meta name="viewport" content="initial-scale=1,maximum-scale=1,user-scalable=no" />
  <style>
    html, body { height: 100%; margin: 0; background: #0f1115; }
    .wrap {
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 24px;
      box-sizing: border-box;
      font-family: Arial, Helvetica, sans-serif;
      color: #fff;
    }
    .card {
      max-width: 520px;
      width: 100%;
      background: rgba(255,255,255,0.06);
      border: 1px solid rgba(255,255,255,0.10);
      border-radius: 18px;
      padding: 18px;
    }
    .title { font-size: 18px; font-weight: 700; margin: 0 0 8px; }
    .text { font-size: 13px; line-height: 1.45; color: rgba(255,255,255,0.80); margin: 0; }
    .code {
      margin-top: 12px;
      font-size: 12px;
      color: rgba(255,255,255,0.90);
      background: rgba(0,0,0,0.35);
      border-radius: 12px;
      padding: 12px;
      overflow-wrap: anywhere;
    }
  </style>
</head>
<body>
  <div class="wrap">
    <div class="card">
      <p class="title">Google Maps API key is required</p>
      <p class="text">
        Set your key in MAD_FINAL/Services/GoogleMapsConfig.cs (ApiKey).
        Then enable billing and the Maps JavaScript API in Google Cloud.
      </p>
      <div class="code">MAD_FINAL/Services/GoogleMapsConfig.cs</div>
    </div>
  </div>
</body>
</html>
""";
    }
}
