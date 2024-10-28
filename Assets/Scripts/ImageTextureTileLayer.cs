using System.Threading;
using System.Threading.Tasks;
using Microsoft.Geospatial;
using Microsoft.Maps.Unity;
using UnityEngine;

public class ImageTextureTileLayer : TextureTileLayer {
    public Texture2D image;
    private byte[] imageData;

    void Start() {
        imageData = ImageConversion.EncodeToPNG(image);
    }

    public override async Task<TextureTile?> GetTexture(TileId tileId, CancellationToken cancellationToken = default) {
        // Load image data from Unity UI image.
        return TextureTile.FromImageData(imageData);
    }
}

// scaling considerations: when zoom in by 1 value, double the pixel count of the image -> same image but more detailed
// random textures: tree, pond, trail, house, etc.
// come back online?, check online status every update, disable and renable the map renderer if come back online?