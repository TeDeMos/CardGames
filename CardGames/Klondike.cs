using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Engine;
using Font = Engine.Font;
// ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable

namespace CardGames;

public class Klondike : CardEngine
{
    [Flags]
    private enum Cheat
    {
        None = 0,
        Reserve = 0b1,
        Foundation = 0b10,
        EmptyColumn = 0b100,
        AcesAndKings = 0b1000,
        Peek = 0b10000
    }

    private static readonly IntRect[] FoundationRects =
    {
        new(297, 5, CardWidth, CardHeight), new(338, 5, CardWidth, CardHeight), new(297, 60, CardWidth, CardHeight),
        new(338, 60, CardWidth, CardHeight)
    };

    private static readonly string[] MenuText =
    {
        "RESET RESERVE", "TAKE CARD FROM FOUNDATION", "PLACE OTHER CARD ON EMPTY TABLEAU COLUMN",
        "ALLOW ACES ON TWOS AND KINGS ON ACES", "PEEK ALL CARDS", "RESTART", "SETUP WIN"
    };

    private static readonly Font Font = new(new(new Bitmap(@"res\font.png"), new(255, 255, 255)),
        Font.ParsePositions(File.ReadAllText(@"res\font.txt")));

    private static readonly Texture FoundationsOutline = new(new Bitmap(@"res\foundations.png"), new(255, 0, 255));
    private static readonly Texture KingOutline = new(new Bitmap(@"res\king.png"), new(255, 0, 255));
    private static readonly Texture Outline = new(new Bitmap(@"res\outline.png"), new(255, 0, 255));
    private static readonly Pixel BackgroundColor = new(0, 120, 0);
    private static readonly Pixel MenuColor = new(180, 180, 180);
    private static readonly Pixel FontColor = new(0, 0, 0);
    private static readonly Pixel SelectedFontColor = new(0, 100, 0);
    private static readonly Pixel WarningFontColor = new(100, 0, 0);
    private static readonly IntRect ReserveRect = new(297, 260, CardWidth, CardHeight);
    private static readonly IntRect WasteRect = new(338, 260, CardWidth, CardHeight);
    private static readonly IntRect FoundationRect = new(297, 5, 2 * CardWidth + 2, 2 * CardHeight + 2);

    private readonly List<Animation> _animating;
    private readonly CardDeck[] _foundations;
    private readonly CardDeck _reserve;
    private readonly CardDeck _selected;
    private readonly CardDeck[] _tableau;
    private readonly List<Animation> _toAnimate;
    private readonly CardDeck _waste;
    private Cheat _cheats;
    private long _animationTimer;
    private bool _auto;
    private int _columnStart;
    private bool _isSelected;
    private bool _menu;
    private Int2 _mouseOffset;
    private int _selectedMenu;
    private Int2 _selectedPosition;
    private int _stage;

    public Klondike(double systemScale) : base(382, 318, 3, 3, systemScale)
    {
        _tableau = new CardDeck[7];
        for (int i = 0; i < _tableau.Length; i++)
            _tableau[i] = new();
        _foundations = new CardDeck[4];
        for (int i = 0; i < _foundations.Length; i++)
            _foundations[i] = new();
        _reserve = new();
        _waste = new();
        _selected = new();
        _selectedPosition = new(0, 0);
        _mouseOffset = new(0, 0);
        _stage = 3;
        _toAnimate = new();
        _animating = new();
        _selectedMenu = -1;
        KeyTracker.TrackKeys(Key.Escape);
    }

    protected override void OnCreate()
    {
        CardDeck deck = CardDeck.CreateDeck();
        deck.Shuffle();
        for (int i = 0; i < 7; i++)
            for (int j = i; j < 7; j++)
                _tableau[j].AddBottom(i == j ? deck.TakeTop().Show() : deck.TakeTop());
        _reserve.AddBottom(deck.TakeAll());
    }

    protected override bool OnLoop(long elapsedMillis)
    {
        if (!_auto)
            HandleMouse();
        Draw(elapsedMillis);
        return true;
    }

    private void HandleMouse()
    {
        Int2 pos = Mouse.Position;
        if (_menu)
        {
            if (KeyTracker[Key.Escape] == State.JustPressed)
            {
                _menu = false;
                return;
            }
            int menuPosition = GetMenuPosition(pos);
            if (Mouse.Left == State.JustPressed && menuPosition > -1)
            {
                _selectedMenu = menuPosition;
                return;
            }
            if (Mouse.Left is State.Released or State.Pressed || _selectedMenu == -1 || _selectedMenu != menuPosition)
                return;
            if (_selectedMenu == 5)
                Restart();
            else if (_selectedMenu == 6)
                SetupWin();
            else
                _cheats ^= (Cheat)(1 << _selectedMenu);
            return;
        }
        bool onReserve = ReserveRect.Contains(pos);
        bool onWaste = WasteRect.Contains(pos);
        bool onFoundation = FoundationRect.Contains(pos);
        bool onTableau = IsOnTableau(pos, out int column, out int row);
        int foundationIndex = (_cheats & Cheat.Foundation) != 0 ? GetFoundationIndex(pos) : -1;
        if (Mouse.Left == State.JustPressed)
        {
            _cheats &= ~Cheat.Peek;
            if (onReserve && _reserve.Empty && (_stage > 1 || (_cheats & Cheat.Reserve) != 0))
            {
                _cheats &= ~Cheat.Reserve;
                _reserve.AddBottom(_waste.TakeAll());
                _reserve.Hide();
                _stage = Math.Max(_stage - 1, 1);
            }
            else if (onReserve && !_reserve.Empty)
            {
                _isSelected = true;
                _selected.AddBottom(_reserve.TakeTop(Math.Min(_stage, _reserve.Length)));
                _mouseOffset = pos - ReserveRect.TopLeft;
                _columnStart = 7;
            }
            else if (onWaste && !_waste.Empty)
            {
                _isSelected = true;
                _selected.AddBottom(_waste.TakeBottom());
                _mouseOffset = pos - WasteRect.TopLeft;
                _columnStart = 8;
            }
            else if (onTableau && !_tableau[column].Empty && _tableau[column][row].Shown)
            {
                _isSelected = true;
                _selected.AddBottom(_tableau[column].TakeBottom(_tableau[column].Length - row));
                _mouseOffset = pos - TableauToCords(column, row);
                _columnStart = column;
            }
            else if ((_cheats & Cheat.Foundation) != 0 && onFoundation && foundationIndex != -1 &&
                     !_foundations[foundationIndex].Empty)
            {
                _cheats &= ~Cheat.Foundation;
                _isSelected = true;
                _selected.AddBottom(_foundations[foundationIndex].TakeBottom());
                _mouseOffset = pos - FoundationRects[foundationIndex].TopLeft;
                _columnStart = 9 + foundationIndex;
            }
            if (_isSelected)
                _selectedPosition = pos - _mouseOffset;
        }
        else if (Mouse.Left == State.Pressed && _isSelected)
            if (_columnStart == 7 && onWaste)
                _selectedPosition = WasteRect.TopLeft;
            else if (_columnStart != 7 && onFoundation && _selected.Length == 1 && CanPlaceFoundation(_selected[0]) &&
                     _columnStart < 9)
                _selectedPosition = FoundationRects[(int)_selected[0].Suit].TopLeft;
            else if (_columnStart != 7 && onTableau && row == _tableau[column].Length - 1 &&
                     CanPlaceTableau(column, _selected[0]) && _columnStart != column)
                _selectedPosition = TableauToCords(column, row + 1);
            else
                _selectedPosition = pos - _mouseOffset;
        else if (Mouse.Left == State.JustReleased && _isSelected)
        {
            if (_columnStart == 7 && onWaste)
            {
                _selected.Show();
                _waste.AddBottom(_selected.TakeAll());
            }
            else if (_columnStart == 7)
                _reserve.AddTop(_selected.TakeAll());
            else if (onFoundation && _selected.Length == 1 && CanPlaceFoundation(_selected[0]))
                _foundations[(int)_selected[0].Suit].AddBottom(_selected.TakeAll());
            else if (onTableau && row == _tableau[column].Length - 1 && CanPlaceTableau(column, _selected[0]))
            {
                if (_tableau[column].Empty && _selected[0].Rank != 13)
                    _cheats &= ~Cheat.EmptyColumn;
                if (_selected[0].Rank == 1 || _selected[0].Rank == 13 && !_tableau[column].Empty)
                    _cheats &= ~Cheat.AcesAndKings;
                _tableau[column].AddBottom(_selected.TakeAll());
            }
            else if (_columnStart == 8)
                _waste.AddBottom(_selected.TakeAll());
            else if (_columnStart >= 9)
            {
                _foundations[_columnStart - 9].AddBottom(_selected.TakeAll());
                _cheats |= Cheat.Foundation;
            }
            else
                _tableau[_columnStart].AddBottom(_selected.TakeAll());
            if (_columnStart < 7 && !_tableau[_columnStart].Empty)
                _tableau[_columnStart][^1] = _tableau[_columnStart][^1].Show();
            _isSelected = false;
            CheckAnimation();
        }
        else if (KeyTracker[Key.Escape] == State.JustPressed)
            _menu = true;
    }

    private void Draw(long elapsedMillis)
    {
        if (_menu)
        {
            Fill(MenuColor);
            for (int i = 0; i < MenuText.Length; i++)
                DrawString(MenuText[i], 50, 19 + 45 * i, Font,
                    i > 4 ? WarningFontColor : (_cheats & (Cheat)(1 << i)) == 0 ? FontColor : SelectedFontColor);
            return;
        }
        Fill(BackgroundColor);
        for (int i = 0; i < _tableau.Length; i++)
        {
            Int2 pos = TableauToCords(i, 0);
            if (_tableau[i].Empty)
            {
                DrawTexture(KingOutline, pos.X, pos.Y);
                continue;
            }
            for (int j = 0; j < _tableau[i].Length; j++)
            {
                pos = TableauToCords(i, j);
                Card c = (_cheats & Cheat.Peek) != 0 ? _tableau[i][j].Show() : _tableau[i][j];
                if (j == _tableau[i].Length - 1)
                    DrawCard(c, pos.X, pos.Y);
                else
                    DrawPartCard(c, CardWidth, 16, pos.X, pos.Y);
            }
        }
        for (int i = 0; i < _foundations.Length; i++)
            if (_foundations[i].Empty)
                DrawTexturePart(FoundationsOutline, i * CardWidth, 0, CardWidth, CardHeight, FoundationRects[i].X,
                    FoundationRects[i].Y);
            else
                DrawCard(_foundations[i][^1], FoundationRects[i].X, FoundationRects[i].Y);
        if (_reserve.Empty)
            DrawTexture(Outline, ReserveRect.X, ReserveRect.Y);
        else
            DrawCard(_reserve[^1], ReserveRect.X, ReserveRect.Y);
        if (_waste.Empty)
            DrawTexture(Outline, WasteRect.X, WasteRect.Y);
        else
            DrawCard(_waste[^1], WasteRect.X, WasteRect.Y);
        if (_auto)
        {
            _animationTimer += elapsedMillis;
            while (_toAnimate.Any() && _toAnimate[0].TimeStart <= _animationTimer)
            {
                _animating.Add(_toAnimate[0]);
                _ = _tableau[_toAnimate[0].Column].TakeBottom();
                _toAnimate.RemoveAt(0);
            }
            while (_animating.Any() && _animating[0].TimeEnd <= _animationTimer)
            {
                _foundations[(int)_animating[0].Card.Suit].AddBottom(_animating[0].Card);
                _animating.RemoveAt(0);
            }
            foreach (Animation animation in _animating)
            {
                Int2 pos = animation.GetPosition(_animationTimer);
                DrawCard(animation.Card, pos.X, pos.Y);
            }
            if (!_animating.Any())
                _auto = false;
        }
        else
            for (int i = 0; i < _selected.Length; i++)
                if (i == _selected.Length - 1)
                    DrawCard(_selected[i], _selectedPosition.X, _selectedPosition.Y + i * 15);
                else
                    DrawPartCard(_selected[i], CardWidth, 16, _selectedPosition.X, _selectedPosition.Y + i * 15);
    }

    private void Restart()
    {
        foreach (CardDeck f in _foundations)
            _ = f.TakeAll();
        _ = _reserve.TakeAll();
        _ = _selected.TakeAll();
        foreach (CardDeck t in _tableau)
            _ = t.TakeAll();
        _ = _waste.TakeAll();
        _isSelected = false;
        _stage = 3;
        _menu = false;
        _auto = false;
        _animationTimer = 0;
        _toAnimate.Clear();
        _animating.Clear();
        _selectedMenu = -1;
        _cheats = Cheat.None;
        OnCreate();
    }

    private void SetupWin()
    {
        _ = _waste.TakeAll();
        _ = _reserve.TakeAll();
        foreach (CardDeck f in _foundations)
            _ = f.TakeAll();
        foreach (CardDeck t in _tableau)
            _ = t.TakeAll();
        for (int i = 13; i >= 2; i--)
            for (int j = 0; j < 4; j++)
                _tableau[j].AddBottom(new Card(i, (Suit)((j + i % 2 * 2) % 4), true));
        _tableau[4].AddBottom(new Card(1, Suit.Hearts, false));
        _tableau[5].AddBottom(new Card(1, Suit.Clubs, true));
        _tableau[6].AddBottom(new Card(1, Suit.Spades, true));
        _tableau[4].AddBottom(new Card(1, Suit.Diamonds, true));
        _menu = false;
    }

    private bool CanPlaceOn(Card a, Card b) => b.Shown && ((_cheats & Cheat.AcesAndKings) != 0 || a.Rank > 1) &&
                                               (a.Rank + 1 == b.Rank || (_cheats & Cheat.AcesAndKings) != 0 &&
                                                   a.Rank == 13 && b.Rank == 1) && OppositeSuit(a, b);

    private static bool OppositeSuit(Card a, Card b) =>
        (a.Suit, b.Suit) is (Suit.Hearts or Suit.Diamonds, Suit.Clubs or Suit.Spades)
        or (Suit.Clubs or Suit.Spades, Suit.Hearts or Suit.Diamonds);

    private static Int2 TableauToCords(int column, int row) => new(5 + column * (CardWidth + 2), 5 + 15 * row);

    private bool CanPlaceTableau(int column, Card c) =>
        _tableau[column].Length == 0 && (c.Rank == 13 || (_cheats & Cheat.EmptyColumn) != 0) ||
        _tableau[column].Length > 0 && CanPlaceOn(c, _tableau[column][^1]);

    private bool CanPlaceFoundation(Card c) => _foundations[(int)c.Suit].Length == c.Rank - 1;

    private bool IsOnTableau(Int2 pos, out int column, out int row)
    {
        column = -1;
        row = -1;
        if (pos.X is < 5 or >= 292)
            return false;
        column = (pos.X - 5) / (CardWidth + 2);
        if ((pos.X - 5) % (CardWidth + 2) >= CardWidth || pos.Y >= 5 + (_tableau[column].Length - 1) * 15 + CardHeight)
            return false;
        row = Math.Min((pos.Y - 5) / 15, _tableau[column].Length - 1);
        return true;
    }

    private static int GetFoundationIndex(Int2 pos)
    {
        for (int i = 0; i < FoundationRects.Length; i++)
            if (FoundationRects[i].Contains(pos))
                return i;
        return -1;
    }

    private static int GetMenuPosition(Int2 pos)
    {
        int result = (pos.Y - 19) / 45;
        return (pos.Y - 19) % 45 is >= 0 and < 7 && pos.X >= 50 && pos.X < 50 + MenuText[result].Length * 6
            ? result
            : -1;
    }

    private void CheckAnimation()
    {
        if (!_reserve.Empty || !_waste.Empty ||
            _tableau.Any(t => !t.Empty && !t[0].Shown || _foundations.Any(f => f.Empty)))
            return;
        _auto = true;
        Dictionary<Card, (int, Int2)> positions = new();
        for (int i = 0; i < _tableau.Length; i++)
            for (int j = 0; j < _tableau[i].Length; j++)
                positions.Add(_tableau[i][j], (i, TableauToCords(i, j)));
        int counter = 0;
        for (int i = 2; i <= 13; i++)
            for (int j = 0; j < 4; j++)
            {
                Card c = new(i, (Suit)j, true);
                if (!positions.ContainsKey(c))
                    continue;
                _toAnimate.Add(new(c, positions[c].Item1, positions[c].Item2, FoundationRects[j].TopLeft,
                    counter * 200, counter * 200 + 500));
                counter++;
            }
    }
}