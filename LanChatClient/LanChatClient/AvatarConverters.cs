using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LanChatClient;

// Zwraca inicjały z Display/User, np. "Tomek 1171 (TOMEK2025)" -> "T1", "SERWER" -> "SE"
public sealed class InitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = (value?.ToString() ?? "").Trim();
        if (s.Length == 0) return "?";

        // wytnij wszystko po "(" jeśli jest
        var idx = s.IndexOf('(');
        if (idx > 0) s = s.Substring(0, idx).Trim();

        // wytnij podwójne spacje
        while (s.Contains("  ", StringComparison.Ordinal)) s = s.Replace("  ", " ");

        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 1 słowo: bierz 2 pierwsze litery
        if (parts.Length == 1)
        {
            var p = parts[0];
            if (p.Length == 1) return p.ToUpperInvariant();
            return (p.Substring(0, 2)).ToUpperInvariant();
        }

        // >=2 słowa: pierwsza litera z 1 i 2
        var a = parts[0].Length > 0 ? parts[0][0].ToString() : "?";
        var b = parts[1].Length > 0 ? parts[1][0].ToString() : "?";
        return (a + b).ToUpperInvariant();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

// Kolor avatara zależny od usera (deterministycznie, żeby każdy miał "swój" kolor)
public sealed class AvatarBrushConverter : IValueConverter
{
    private static readonly Color[] Palette =
    {
        Color.FromRgb(0x12, 0x8C, 0x7E), // whatsapp teal
        Color.FromRgb(0x25, 0xD3, 0x66), // green
        Color.FromRgb(0x34, 0xB7, 0xF1), // blue
        Color.FromRgb(0xF2, 0xA1, 0x54), // orange
        Color.FromRgb(0xB5, 0x7E, 0xDC), // purple
        Color.FromRgb(0xE3, 0x5D, 0x6A), // red-ish
        Color.FromRgb(0x6C, 0xB2, 0x6A), // green 2
        Color.FromRgb(0x5B, 0x9B, 0xD5), // blue 2
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = (value?.ToString() ?? "").Trim();
        if (s.Length == 0) return new SolidColorBrush(Palette[0]);

        unchecked
        {
            int h = 23;
            foreach (var ch in s)
                h = h * 31 + ch;

            var idx = Math.Abs(h) % Palette.Length;
            return new SolidColorBrush(Palette[idx]);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}