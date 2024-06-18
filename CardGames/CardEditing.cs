using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Engine;
using Font = Engine.Font;
// ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable

namespace CardGames;

public class CardEditing : CardEngine
{
    private static readonly Texture RawCards = new(new Bitmap(@"res\cardsRaw.png"));

    private static readonly Font Font = new(new(new Bitmap(@"res\font.png")),
        Font.ParsePositions(File.ReadAllText(@"res\font.txt")));

    private static readonly Pixel White = new(255, 255, 255);
    private static readonly Pixel Gray = new(180, 180, 180);
    private static readonly IntRect[] Colors;
    private static readonly IntRect Up = new(10, 215, 30, 7);
    private static readonly IntRect Down = new(10, 290, 30, 7);
    private int _index;

    static CardEditing()
    {
        Colors = new IntRect[17];
        Colors[0] = new(169, 215, 20, 83);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                Colors[i * 4 + j + 1] = new(190 + i * 90, 215 + j * 21, 89, 20);
    }

    private readonly Selected _s = new();

    public CardEditing(int pixelWidth, int pixelHeight, double systemScale) : base(550, 300, pixelWidth, pixelHeight,
        systemScale) => KeyTracker.TrackKeys(Key.Space);

    protected override void OnCreate() => Draw();

    protected override bool OnLoop(long elapsedMillis)
    {
        if (HandleMouse())
            Draw();
        return true;
    }

    private bool HandleMouse()
    {
        if (KeyTracker[Key.Space] == State.JustPressed)
            GetTexture(0, 0, 546, 212).ToBitmap().Save(@"C:\users\tedem\result.png");
        if (Mouse.Left != State.JustPressed)
            return false;
        if (Up.Contains(Mouse.Position))
        {
            _index--;
            if (_index < 0)
                _index = 42;
            return true;
        }
        if (Down.Contains(Mouse.Position))
        {
            _index++;
            if (_index > 42)
                _index = 0;
            return true;
        }
        for (int i = 0; i < Colors.Length; i++)
            if (Colors[i].Contains(Mouse.Position))
            {
                _s.Set(_index, i - 1);
                return true;
            }
        return false;
    }

    private void Draw()
    {
        Fill(0);
        DrawAllCards();
        DrawScrollMenu();
        DrawColorMenu();
    }

    private void DrawColorMenu()
    {
        for (int i = 0; i < Colors.Length; i++)
        {
            IntRect r = Colors[i];
            if (i == _s.Get(_index) + 1)
                DrawRect(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2, White);
            DrawRect(Colors[i], Nord.Get(i - 1));
        }
    }

    private void DrawScrollMenu()
    {
        DrawAllCards();
        string[] names = _s.GetAround(_index);
        DrawString("UP", 10, 215, Font, White);
        for (int i = 0; i < 5; i++)
            DrawString(names[i], 10, 230 + 11 * i, Font, i == 2 ? White : Gray);
        DrawString("DOWN", 10, 290, Font, White);
    }

    private void DrawAllCards()
    {
        DrawRedAce(0);
        DrawRedAce(1);
        DrawBlackAce(2);
        DrawBlackAce(3);
        for (int i = 1; i <= 9; i++)
        {
            DrawRedNumber(i, 0);
            DrawRedNumber(i, 1);
            DrawBlackNumber(i, 2);
            DrawBlackNumber(i, 3);
        }
        DrawRedJack(0);
        DrawRedJack(1);
        DrawBlackJack(2);
        DrawBlackJack(3);
        DrawHeartQueen();
        DrawDiamondQueen();
        DrawClubQueen();
        DrawSpadeQueen();
        DrawRedKing(0);
        DrawRedKing(1);
        DrawBlackKing(2);
        DrawBlackKing(3);
        DrawRedJoker();
        DrawBlackJoker();
        DrawBlueReverse();
        DrawRedReverse();
    }

    private void DrawRedAce(int y) => DrawReplacing(0, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankRed, _s.SuitSmallRed, _s.SuitLargeRed));

    private void DrawBlackAce(int y) => DrawReplacing(0, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankBlack, _s.SuitSmallBlack, _s.SuitLargeBlack));

    private void DrawRedNumber(int x, int y) => DrawReplacing(x, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankRed, _s.SuitSmallRed, _s.SuitRegularRed));

    private void DrawBlackNumber(int x, int y) => DrawReplacing(x, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankBlack, _s.SuitSmallBlack, _s.SuitRegularBlack));

    private void DrawRedJack(int y) => DrawReplacing(10, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankRed, _s.SuitSmallRed, _s.ClothesFillARed,
            _s.ClothesFillBRed, _s.ClothesOutlineRed, _s.JackHatFillRed, _s.JackHatOutlineRed, _s.SkinFillRed,
            _s.SkinOutlineRed, _s.EyesRed));

    private void DrawBlackJack(int y) => DrawReplacing(10, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankBlack, _s.SuitSmallBlack, _s.ClothesFillABlack,
            _s.ClothesFillBBlack, _s.ClothesOutlineBlack, _s.JackHatFillBlack, _s.JackHatOutlineBlack,
            _s.SkinFillBlack, _s.SkinOutlineBlack, _s.EyesBlack));

    private void DrawHeartQueen() => DrawReplacing(11, 0,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankRed, _s.SuitSmallRed, _s.ClothesFillARed,
            _s.ClothesFillBRed, _s.ClothesOutlineRed, _s.QueenHairMainHearts, _s.QueenHairDetailHearts, _s.SkinFillRed,
            _s.SkinOutlineRed, _s.EyesRed));

    private void DrawDiamondQueen() => DrawReplacing(11, 1,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankRed, _s.SuitSmallRed, _s.ClothesFillARed,
            _s.ClothesFillBRed, _s.ClothesOutlineRed, _s.QueenHairMainDiamonds, _s.QueenHairDetailDiamonds,
            _s.SkinFillRed, _s.SkinOutlineRed, _s.EyesRed));

    private void DrawClubQueen() => DrawReplacing(11, 2,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankBlack, _s.SuitSmallBlack, _s.ClothesFillABlack,
            _s.ClothesFillBBlack, _s.ClothesOutlineBlack, _s.QueenHairMainClubs, _s.QueenHairDetailClubs,
            _s.SkinFillBlack, _s.SkinOutlineBlack, _s.EyesBlack));

    private void DrawSpadeQueen() => DrawReplacing(11, 3,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankBlack, _s.SuitSmallBlack, _s.ClothesFillABlack,
            _s.ClothesFillBBlack, _s.ClothesOutlineBlack, _s.QueenHairMainSpades, _s.QueenHairDetailSpades,
            _s.SkinFillBlack, _s.SkinOutlineBlack, _s.EyesBlack));

    private void DrawRedKing(int y) => DrawReplacing(12, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankRed, _s.SuitSmallRed, _s.ClothesFillARed,
            _s.ClothesFillBRed, _s.ClothesOutlineRed, _s.KingCrownFillRed, _s.KingCrownDetailRed, _s.SkinFillRed,
            _s.SkinOutlineRed, _s.EyesRed));

    private void DrawBlackKing(int y) => DrawReplacing(12, y,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankBlack, _s.SuitSmallBlack, _s.ClothesFillABlack,
            _s.ClothesFillBBlack, _s.ClothesOutlineBlack, _s.KingCrownFillBlack, _s.KingCrownDetailBlack,
            _s.SkinFillBlack, _s.SkinOutlineBlack, _s.EyesBlack));

    private void DrawRedJoker() => DrawReplacing(13, 0,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankRed, _s.ClothesFillARed, _s.ClothesFillBRed,
            _s.ClothesOutlineRed, _s.SkinFillRed, _s.SkinOutlineRed, _s.EyesRed));

    private void DrawBlackJoker() => DrawReplacing(13, 1,
        ToColors(_s.Transparent, _s.Edge, _s.Background, _s.RankBlack, _s.ClothesFillABlack, _s.ClothesFillBBlack,
            _s.ClothesOutlineBlack, _s.SkinFillBlack, _s.SkinOutlineBlack, _s.EyesBlack));

    private void DrawBlueReverse() => DrawReplacing(13, 2,
        ToColors(_s.Transparent, _s.Edge, _s.ReverseBackgroundBlue, _s.ReverseDetailBlue));

    private void DrawRedReverse() => DrawReplacing(13, 3,
        ToColors(_s.Transparent, _s.Edge, _s.ReverseBackgroundRed, _s.ReverseDetailRed));

    private static Pixel[] ToColors(params int[] indices) => indices.Select(Nord.Get).ToArray();

    private void DrawReplacing(int x, int y, Pixel[] colors)
    {
        (int x, int y) pos = GetPos(x, y);
        for (int i = 0; i < 39; i++)
            for (int j = 0; j < 53; j++)
                DrawPixel(colors[GetPixelIndex(RawCards[pos.x + i, pos.y + j] ?? new(0, 0, 0))], pos.x + i, pos.y + j);
    }

    private static (int, int) GetPos(int x, int y) => (39 * x, 53 * y);

    private static int GetPixelIndex(Pixel p)
    {
        int result = p.R == 128 || p.B == 128 || p.G == 128 ? 7 : 0;
        if (p.R > 0)
            result += 4;
        if (p.G > 0)
            result += 2;
        if (p.B > 0)
            result++;
        Console.WriteLine(result);
        return result;
    }

    private class Selected
    {
        private static readonly Random R = new();
        public int Transparent { get; set; } = -1;
        public int Edge { get; set; } = R.Next(16);
        public int Background { get; set; } = R.Next(16);
        public int RankRed { get; set; } = R.Next(16);
        public int RankBlack { get; set; } = R.Next(16);
        public int SuitSmallRed { get; set; } = R.Next(16);
        public int SuitSmallBlack { get; set; } = R.Next(16);
        public int SuitRegularRed { get; set; } = R.Next(16);
        public int SuitRegularBlack { get; set; } = R.Next(16);
        public int SuitLargeRed { get; set; } = R.Next(16);
        public int SuitLargeBlack { get; set; } = R.Next(16);
        public int ClothesFillARed { get; set; } = R.Next(16);
        public int ClothesFillABlack { get; set; } = R.Next(16);
        public int ClothesFillBRed { get; set; } = R.Next(16);
        public int ClothesFillBBlack { get; set; } = R.Next(16);
        public int ClothesOutlineRed { get; set; } = R.Next(16);
        public int ClothesOutlineBlack { get; set; } = R.Next(16);
        public int SkinFillRed { get; set; } = R.Next(16);
        public int SkinFillBlack { get; set; } = R.Next(16);
        public int SkinOutlineRed { get; set; } = R.Next(16);
        public int SkinOutlineBlack { get; set; } = R.Next(16);
        public int EyesRed { get; set; } = R.Next(16);
        public int EyesBlack { get; set; } = R.Next(16);
        public int JackHatFillRed { get; set; } = R.Next(16);
        public int JackHatFillBlack { get; set; } = R.Next(16);
        public int JackHatOutlineRed { get; set; } = R.Next(16);
        public int JackHatOutlineBlack { get; set; } = R.Next(16);
        public int QueenHairMainHearts { get; set; } = R.Next(16);
        public int QueenHairMainDiamonds { get; set; } = R.Next(16);
        public int QueenHairMainClubs { get; set; } = R.Next(16);
        public int QueenHairMainSpades { get; set; } = R.Next(16);
        public int QueenHairDetailHearts { get; set; } = R.Next(16);
        public int QueenHairDetailDiamonds { get; set; } = R.Next(16);
        public int QueenHairDetailClubs { get; set; } = R.Next(16);
        public int QueenHairDetailSpades { get; set; } = R.Next(16);
        public int KingCrownFillRed { get; set; } = R.Next(16);
        public int KingCrownFillBlack { get; set; } = R.Next(16);
        public int KingCrownDetailRed { get; set; } = R.Next(16);
        public int KingCrownDetailBlack { get; set; } = R.Next(16);
        public int ReverseBackgroundRed { get; set; } = R.Next(16);
        public int ReverseBackgroundBlue { get; set; } = R.Next(16);
        public int ReverseDetailRed { get; set; } = R.Next(16);
        public int ReverseDetailBlue { get; set; } = R.Next(16);
        private readonly PropertyInfo[] _properties = typeof(Selected).GetProperties();

        public string[] GetAround(int index)
        {
            string[] result = new string[5];
            for (int i = 0; i < 5; i++)
                result[i] = _properties[(index + i - 2 + _properties.Length) % _properties.Length].Name.ToUpper();
            return result;
        }

        public void Set(int propertyIndex, int colorIndex) => _properties[propertyIndex].SetValue(this, colorIndex);
        public int Get(int propertyIndex) => (int)(_properties[propertyIndex].GetValue(this) ?? -1);
    }

    private static class Nord
    {
        public static Pixel Get(int index) => index >= 0 ? Theme[index] : Transparent;

        private static readonly Pixel[] Theme =
        {
            new(46, 52, 64), new(59, 66, 82), new(67, 76, 94), new(76, 86, 106), new(216, 222, 233),
            new(229, 233, 240), new(236, 239, 244), new(143, 188, 187), new(136, 192, 208), new(129, 161, 193),
            new(94, 129, 172), new(191, 97, 106), new(208, 135, 112), new(235, 203, 139), new(163, 190, 140),
            new(180, 142, 173)
        };

        private static readonly Pixel Transparent = new(255, 0, 255);
    }
}