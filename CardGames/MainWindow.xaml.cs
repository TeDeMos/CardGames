using System.Windows;

namespace CardGames;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //Klondike s = new(1.25);
        Spider s = new(1.25);
        //CardEditing s = new(3, 3, 1.25);
        s.Start();
        Close();
    }
}