using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Engine;

namespace CardGames;

public abstract class CardEngine : PixelEngine
{
    protected const int CardWidth = 39;
    protected const int CardHeight = 53;
    private static readonly Texture Cards = new(new Bitmap(@"res\cards.png"), new(255, 0, 255));

    protected CardEngine(int width, int height, int pixelWidth, int pixelHeight, double systemScale) : base(width,
        height, pixelWidth, pixelHeight, systemScale) { }

    protected void DrawCard(Card c, int x, int y)
    {
        if (c.Shown)
            DrawTexturePart(Cards, (c.Rank - 1) * 39, (int)c.Suit * 53, 39, 53, x, y);
        else
            DrawTexturePart(Cards, 507, 159, 39, 53, x, y);
    }

    protected void DrawPartCard(Card c, int width, int height, int x, int y)
    {
        if (c.Shown)
            DrawTexturePart(Cards, (c.Rank - 1) * 39, (int)c.Suit * 53, width, height, x, y);
        else
            DrawTexturePart(Cards, 507, 159, width, height, x, y);
    }

    protected void DrawCardDarkened(Card c, int x, int y, double multiplier)
    {
        if (c.Shown)
            BlendTexturePart(Cards, (c.Rank - 1) * 39, (int)c.Suit * 53, 39, 53, x, y,
                (p, q) => Darken(p, q, multiplier));
        else
            BlendTexturePart(Cards, 507, 159, 39, 53, x, y, (p, q) => Darken(p, q, multiplier));
    }

    protected void DrawPartCardDarkened(Card c, int width, int height, int x, int y, double multiplier)
    {
        if (c.Shown)
            BlendTexturePart(Cards, (c.Rank - 1) * 39, (int)c.Suit * 53, width, height, x, y,
                (p, q) => Darken(p, q, multiplier));
        else
            BlendTexturePart(Cards, 507, 159, width, height, x, y, (p, q) => Darken(p, q, multiplier));
    }

    private static Pixel Darken(Pixel? t, Pixel b, double multiplier) => t.HasValue
        ? new((byte)(t.Value.R * multiplier), (byte)(t.Value.G * multiplier), (byte)(t.Value.B * multiplier))
        : b;

    protected enum Suit
    {
        Hearts = 0,
        Diamonds = 1,
        Clubs = 2,
        Spades = 3
    }

    protected record Card(int Rank, Suit Suit, bool Shown)
    {
        public Card Show() => this with { Shown = true };

        public Card Hide() => this with { Shown = false };
    }

    protected class CardDeck
    {
        private List<Card> _cards;

        public CardDeck() => _cards = new();

        private CardDeck(IEnumerable<Card> l) => _cards = l.ToList();

        public Card this[Index index] { get => _cards[index]; set => _cards[index] = value; }

        public int Length => _cards.Count;
        public bool Empty => Length == 0;

        public static CardDeck CreateDeck()
        {
            CardDeck result = new();
            for (int j = 0; j < 4; j++)
                for (int i = 1; i <= 13; i++)
                    result.AddBottom(new Card(i, (Suit)j, false));
            return result;
        }

        public void Shuffle()
        {
            Random random = new();
            _cards = _cards.OrderBy(_ => random.Next()).ToList();
        }

        public void Show()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i] = _cards[i].Show();
        }

        public void Hide()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i] = _cards[i].Hide();
        }

        public void AddBottom(Card c) => _cards.Add(c);

        public void AddBottom(CardDeck d) => _cards.AddRange(d._cards);

        public void AddTop(Card c) => _cards.Insert(0, c);

        public void AddTop(CardDeck d) => _cards.InsertRange(0, d._cards);

        public Card TakeTop()
        {
            Card c = _cards[0];
            _cards.RemoveAt(0);
            return c;
        }

        public CardDeck TakeTop(int n)
        {
            CardDeck d = new(_cards.Take(n));
            _cards.RemoveRange(0, n);
            return d;
        }

        public Card TakeBottom()
        {
            Card c = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);
            return c;
        }

        public CardDeck TakeBottom(int n)
        {
            CardDeck d = new(_cards.TakeLast(n));
            _cards.RemoveRange(Length - n, n);
            return d;
        }

        public CardDeck TakeAll()
        {
            CardDeck d = new(new List<Card>(_cards));
            _cards.Clear();
            return d;
        }
    }

    protected record Animation(Card Card, int Column, Int2 Start, Int2 End, long TimeStart, long TimeEnd)
    {
        public Int2 GetPosition(long time)
        {
            if (time < TimeStart)
                return Start;
            if (time > TimeEnd)
                return End;
            return Start + (End - Start) * (int)(time - TimeStart) / (int)(TimeEnd - TimeStart);
        }
    }
}