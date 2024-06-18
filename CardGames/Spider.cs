using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Engine;
using Font = Engine.Font;

namespace CardGames;

public class Spider : CardEngine
{
    [Flags]
    private enum Cheat
    {
        None = 0,
        Color = 0b1,
        Order = 0b10,
        Kings = 0b100,
        Peek = 0b1000
    }

    private static readonly Pixel BackgroundColor = new(76, 86, 106); //new(94, 129, 172); //new(0, 120, 0);
    private static readonly Pixel MenuColor = new(180, 180, 180);
    private static readonly Pixel FontColor = new(0, 0, 0);
    private static readonly Pixel SelectedFontColor = new(0, 100, 0);
    private static readonly Pixel WarningFontColor = new(100, 0, 0);
    private static readonly Texture Outline = new(new Bitmap(@"res\outline.png"), new(255, 0, 255));

    private static readonly Font Font = new(new(new Bitmap(@"res\font.png"), new(255, 255, 255)),
        Font.ParsePositions(File.ReadAllText(@"res\font.txt")));

    private static readonly IntRect ReserveRect = new(418, 262, CardWidth, CardHeight);
    private static readonly Int2 FoundationVector = new(418, 5);

    private static readonly string[] MenuText =
    {
        "IGNORE COLORS", "IGNORE ORDER", "ALLOW KINGS ON ACES", "PEEK ALL CARDS",
        "MOVE 2 FOUNDATION STACKS TO RESERVE", "RESTART 1 COLOR", "RESTART 2 COLORS", "RESTART 4 COLORS", "SETUP WIN"
    };

    private int _colors;
    private readonly CardDeck _foundation;
    private readonly CardDeck _reserve;
    private readonly CardDeck _selected;
    private readonly CardDeck[] _tableau;
    private int _columnStart;
    private bool _isSelected;
    private Int2 _mouseOffset;
    private Int2 _selectedPosition;
    private int _auto;
    private long _animationTimer;
    private readonly List<Animation> _toAnimate;
    private readonly List<Animation> _animating;
    private bool _menu;
    private int _selectedMenu;
    private Cheat _cheats;

    public Spider(double systemScale) : base(503, 320, 3, 3, systemScale)
    {
        _tableau = new CardDeck[10];
        for (int i = 0; i < _tableau.Length; i++)
            _tableau[i] = new();
        _foundation = new();
        _reserve = new();
        _selected = new();
        _mouseOffset = 0;
        _selectedPosition = 0;
        _colors = 2;
        _toAnimate = new();
        _animating = new();
        KeyTracker.TrackKeys(Key.Escape);
    }

    protected override void OnCreate()
    {
        CardDeck deck = CardDeck.CreateDeck();
        deck.AddBottom(CardDeck.CreateDeck());
        deck.Shuffle();
        for (int i = 0; i < 54; i++)
            _tableau[i % 10].AddBottom(i >= 44 ? deck.TakeTop().Show() : deck.TakeTop());
        _reserve.AddBottom(deck.TakeAll());
    }

    protected override bool OnLoop(long elapsedMillis)
    {
        if (_auto == 0)
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
            if (_selectedMenu == 4)
                MoveFoundation();
            else if (_selectedMenu is >= 5 and <= 7)
                Restart((int)Math.Pow(2, _selectedMenu - 5));
            else if (_selectedMenu == 8)
                SetupWin();
            else
                _cheats ^= (Cheat)(1 << _selectedMenu);
            return;
        }
        bool onTableau = IsOnTableau(pos, out int column, out int row);
        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        bool onReserve = ReserveRect.Contains(pos);
        if (Mouse.Left == State.JustPressed)
        {
            _cheats &= ~Cheat.Peek;
            if (onReserve && !_reserve.Empty)
            {
                for (int i = 0; i < _tableau.Length && i < _reserve.Length; i++)
                    _toAnimate.Add(new(_reserve[i].Show(), i, ReserveRect.TopLeft,
                        TableauToCords(i, _tableau[i].Length), i * 200, i * 200 + 500));
                _auto = 1;
                _animationTimer = 0;
            }
            else if (onTableau && !_tableau[column].Empty && _tableau[column][row].Shown &&
                     AllGood(column, row, false))
            {
                _isSelected = true;
                _mouseOffset = pos - TableauToCords(column, row);
                _selected.AddBottom(_tableau[column].TakeBottom(_tableau[column].Length - row));
                _columnStart = column;
            }
            if (_isSelected)
                _selectedPosition = pos - _mouseOffset;
        }
        else if (Mouse.Left == State.Pressed && _isSelected)
            _selectedPosition
                = onTableau && row == _tableau[column].Length - 1 && CanPlaceTableau(column, _selected[0]) &&
                  _columnStart != column
                    ? TableauToCords(column, row + 1)
                    : pos - _mouseOffset;
        else if (Mouse.Left == State.JustReleased && _isSelected)
        {
            if (onTableau && row == _tableau[column].Length - 1 && CanPlaceTableau(column, _selected[0]))
            {
                if (!OrderGood())
                    _cheats &= ~Cheat.Order;
                if (!ColorsGood())
                    _cheats &= ~Cheat.Color;
                if (!_tableau[column].Empty && _selected[0].Rank == 13)
                    _cheats &= ~Cheat.Kings;
                _tableau[column].AddBottom(_selected.TakeAll());
            }
            else
                _tableau[_columnStart].AddBottom(_selected.TakeAll());
            if (!_tableau[_columnStart].Empty)
                _tableau[_columnStart][^1] = _tableau[_columnStart][^1].Show();
            RemoveCompleted();
            _isSelected = false;
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
                DrawString(MenuText[i], 50, 14 + 35 * i, Font,
                    i > 3 ? WarningFontColor : (_cheats & (Cheat)(1 << i)) == 0 ? FontColor : SelectedFontColor);
            return;
        }
        Fill(BackgroundColor);
        for (int i = 0; i < _tableau.Length; i++)
        {
            Int2 pos = TableauToCords(i, 0);
            if (_tableau[i].Empty)
                DrawTexture(Outline, pos.X, pos.Y);
            else
            {
                int[] levels = new int[_tableau[i].Length];
                for (int j = _tableau[i].Length - 2; j >= 0; j--)
                {
                    if (!_tableau[i][j].Shown)
                        break;
                    levels[j] = levels[j + 1];
                    if ((_cheats & Cheat.Color) == 0 && !SameColor(_tableau[i][j], _tableau[i][j + 1]) ||
                        (_cheats & Cheat.Order) == 0 && _tableau[i][j].Rank != _tableau[i][j + 1].Rank + 1)
                        levels[j]++;
                }
                for (int j = 0; j < _tableau[i].Length; j++)
                {
                    pos = TableauToCords(i, j);
                    Card c = (_cheats & Cheat.Peek) != 0 ? _tableau[i][j].Show() : _tableau[i][j];
                    if (levels[j] != 0)
                        DrawPartCardDarkened(c, CardWidth, 16, pos.X, pos.Y, 1 / (0.25 + levels[j]));
                    else if (j == _tableau[i].Length - 1)
                        DrawCard(c, pos.X, pos.Y);
                    else
                        DrawPartCard(c, CardWidth, 16, pos.X, pos.Y);
                }
            }
        }
        for (int i = 0; i < (_reserve.Length + 9) / 10; i++)
            DrawCard(_reserve[0], ReserveRect.X + ((_reserve.Length + 9) / 10 - 1 - i) * ((CardWidth + 2) / 4),
                ReserveRect.Y);
        for (int i = 12; i < _foundation.Length; i += 13)
            DrawCard(_foundation[i],
                FoundationVector.X + ((_foundation.Length + 12) / 13 - 1 - i / 13) * ((CardWidth + 2) / 8),
                FoundationVector.Y);
        if (!_foundation.Empty)
            DrawCard(_foundation[^1], FoundationVector.X, FoundationVector.Y);
        if (_auto > 0)
        {
            _animationTimer += elapsedMillis;
            while (_toAnimate.Any() && _toAnimate[0].TimeStart <= _animationTimer)
            {
                if (_auto == 2)
                    _animating.Add(_toAnimate[0] with
                    {
                        Start = TableauToCords(_toAnimate[0].Column, _tableau[_toAnimate[0].Column].Length - 1)
                    });
                else
                    _animating.Add(_toAnimate[0]);
                if (_auto == 1)
                    _ = _reserve.TakeTop();
                else
                {
                    _ = _tableau[_toAnimate[0].Column].TakeBottom();
                    if (!_tableau[_toAnimate[0].Column].Empty)
                        _tableau[_toAnimate[0].Column][^1] = _tableau[_toAnimate[0].Column][^1].Show();
                }
                _toAnimate.RemoveAt(0);
            }
            while (_animating.Any() && _animating[0].TimeEnd <= _animationTimer)
            {
                if (_auto == 1)
                    _tableau[_animating[0].Column].AddBottom(_animating[0].Card);
                else
                    _foundation.AddBottom(_animating[0].Card);
                _animating.RemoveAt(0);
            }
            foreach (Animation animation in _animating)
            {
                Int2 pos = animation.GetPosition(_animationTimer);
                DrawCard(animation.Card, pos.X, pos.Y);
            }
            if (!_animating.Any())
                _auto = 0;
        }
        else
            for (int i = 0; i < _selected.Length; i++)
                if (i == _selected.Length - 1)
                    DrawCard(_selected[i], _selectedPosition.X, _selectedPosition.Y + i * 15);
                else
                    DrawPartCard(_selected[i], CardWidth, 16, _selectedPosition.X, _selectedPosition.Y + i * 15);
    }

    private void RemoveCompleted()
    {
        int n = 0;
        for (int i = 0; i < _tableau.Length; i++)
            if (_tableau[i].Length >= 13 && AllGood(i, _tableau[i].Length - 13, true))
            {
                for (int j = 0; j < 13; j++)
                {
                    _toAnimate.Add(new(_tableau[i][^(j + 1)], i, TableauToCords(i, _tableau[i].Length - 1 - j),
                        FoundationVector, n * 200, n * 200 + 500));
                    n++;
                }
                _auto = 2;
                _animationTimer = 0;
            }
    }

    private bool AllGood(int column, int row, bool ignoreCheats)
    {
        if (!_tableau[column][row].Shown)
            return false;
        for (int i = row + 1; i < _tableau[column].Length; i++)
            if (!_tableau[column][i].Shown ||
                (ignoreCheats || (_cheats & Cheat.Color) == 0) &&
                !SameColor(_tableau[column][i], _tableau[column][row]) ||
                (ignoreCheats || (_cheats & Cheat.Order) == 0) &&
                _tableau[column][i].Rank - _tableau[column][row].Rank != row - i)
                return false;
        return true;
    }

    private bool OrderGood()
    {
        for (int i = 1; i < _selected.Length; i++)
            if (_selected[0].Rank - _selected[i].Rank != -i)
                return false;
        return true;
    }

    private bool ColorsGood()
    {
        for (int i = 1; i < _selected.Length; i++)
            if (!SameColor(_selected[i], _selected[0]))
                return false;
        return true;
    }

    private bool SameColor(Card a, Card b) => _colors switch
    {
        4 => a.Suit == b.Suit,
        2 => (a.Suit, b.Suit) is (Suit.Hearts or Suit.Diamonds, Suit.Hearts or Suit.Diamonds)
            or (Suit.Clubs or Suit.Spades, Suit.Clubs or Suit.Spades),
        _ => true
    };

    private void Restart(int colors)
    {
        _ = _foundation.TakeAll();
        _ = _reserve.TakeAll();
        foreach (CardDeck t in _tableau)
            _ = t.TakeAll();
        _ = _selected.TakeAll();
        _isSelected = false;
        _menu = false;
        _auto = 0;
        _animationTimer = 0;
        _toAnimate.Clear();
        _animating.Clear();
        _selectedMenu = -1;
        _cheats = Cheat.None;
        _colors = colors;
        OnCreate();
    }

    private void SetupWin()
    {
        _ = _reserve.TakeAll();
        _ = _foundation.TakeAll();
        foreach (CardDeck t in _tableau)
            _ = t.TakeAll();
        for (int i = 13; i >= 2; i--)
            for (int j = 0; j < 8; j++)
                _tableau[j].AddBottom(new Card(i, (Suit)(j % 4), true));
        for (int i = 0; i < 4; i++)
        {
            _tableau[8].AddBottom(new Card(1, (Suit)i, true));
            _tableau[9].AddBottom(new Card(1, (Suit)i, true));
        }
        _menu = false;
    }

    private void MoveFoundation()
    {
        if (_foundation.Length < 26)
            return;
        CardDeck taken = _foundation.TakeBottom(26);
        taken.Hide();
        taken.Shuffle();
        _reserve.AddBottom(taken.TakeAll());
    }

    private int RowSize(int column) => _tableau[column].Empty
        ? 15
        : Math.Min(15, (Height - 10 - CardHeight) / _tableau[column].Length + 1);

    private Int2 TableauToCords(int column, int row) =>
        new(5 + column * (CardWidth + 2), 5 + RowSize(column) * row);

    private bool IsOnTableau(Int2 pos, out int column, out int row)
    {
        column = -1;
        row = -1;
        if (pos.X is < 5 or >= 413)
            return false;
        column = (pos.X - 5) / (CardWidth + 2);
        if ((pos.X - 5) % (CardWidth + 2) >= CardWidth ||
            pos.Y >= 5 + (_tableau[column].Length - 1) * RowSize(column) + CardHeight)
            return false;
        row = Math.Min((pos.Y - 5) / RowSize(column), _tableau[column].Length - 1);
        return true;
    }

    private bool CanPlaceTableau(int column, Card c) => _tableau[column].Length == 0 || _tableau[column].Length > 0 &&
        (_tableau[column][^1].Rank == c.Rank + 1 ||
         (_cheats & Cheat.Kings) != 0 && _tableau[column][^1].Rank == 1 && c.Rank == 13);

    private static int GetMenuPosition(Int2 pos)
    {
        int result = (pos.Y - 14) / 35;
        return (pos.Y - 14) % 35 is >= 0 and < 7 && pos.X >= 50 && pos.X < 50 + MenuText[result].Length * 6
            ? result
            : -1;
    }
}