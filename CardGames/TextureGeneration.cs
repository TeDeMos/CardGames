using System.Drawing;

namespace CardGames;

public static class TextureGeneration
{
    public static void Generate()
    {
        Bitmap template = new(@"res\template.png");
        Bitmap textures = new(@"res\textures.png");
        Bitmap result = new(template.Width, template.Height * 4);
        Graphics.FromImage(result).Clear(Color.FromArgb(1, 1, 1));
        for (int n = 0; n < 4; n++)
            for (int y = 0; y < template.Height; y++)
            {
                int counter = 0;
                for (int x = 0; x < template.Width; x++)
                {
                    Color c = template.GetPixel(x, y);
                    bool reverse = c.R + c.G + c.B == 510;
                    switch (c.R, c.G, c.B)
                    {
                        case (255, 0, 0) or (0, 255, 255):
                            for (int i = 0; i < 5; i++)
                                for (int j = 0; j < 5; j++)
                                {
                                    Color t = reverse
                                        ? textures.GetPixel(counter * 5 + 4 - i, 4 - j)
                                        : textures.GetPixel(counter * 5 + i, j);
                                    if ((t.R, t.G, t.B) == (0, 0, 0) && n < 2)
                                        t = Color.Red;
                                    result.SetPixel(x + i, n * template.Height + y + j, t);
                                }
                            counter++;
                            break;
                        case (0, 255, 0) or (255, 0, 255):
                            for (int i = 0; i < 5; i++)
                                for (int j = 0; j < 5; j++)
                                    result.SetPixel(x + i, n * template.Height + y + j,
                                        reverse
                                            ? textures.GetPixel(n * 5 + i, 9 - j)
                                            : textures.GetPixel(n * 5 + i, 5 + j));
                            break;
                        case (0, 0, 255) or (255, 255, 0):
                            for (int i = 0; i < 7; i++)
                                for (int j = 0; j < 7; j++)
                                    result.SetPixel(x + i, n * template.Height + y + j,
                                        reverse
                                            ? textures.GetPixel(n * 7 + i, 16 - j)
                                            : textures.GetPixel(n * 7 + i, 10 + j));
                            break;
                        default:
                            Color o = result.GetPixel(x, n * template.Height + y);
                            if ((o.R, o.G, o.B) == (1, 1, 1))
                                result.SetPixel(x, n * template.Height + y, c);
                            break;
                    }
                }
            }
        result.Save("result.png");
    }
}