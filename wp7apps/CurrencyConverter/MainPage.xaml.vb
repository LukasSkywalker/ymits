Imports System.Text.RegularExpressions
Partial Public Class MainPage
    Inherits PhoneApplicationPage
    Private wc As WebClient

    ' Constructor
    Public Sub New()
        InitializeComponent()

        AddItems()

        wc = New WebClient()
        wc.Encoding = System.Text.Encoding.UTF8

        AddHandler wc.OpenReadCompleted, New OpenReadCompletedEventHandler(AddressOf wc_OpenReadCompleted)
    End Sub

    Private Sub Button1_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button1.Click
        If FromListBox.SelectedIndex = -1 Then
            MessageBox.Show("Please select a source currency.", "Error", MessageBoxButton.OKCancel)
        ElseIf ToListBox.SelectedIndex = -1 Then
            MessageBox.Show("Please select a target currency.", "Error", MessageBoxButton.OKCancel)
        Else
            Dim source As String = FromListBox.SelectedValue
            source = source.Substring(0, 3)
            Dim target As String = ToListBox.SelectedValue
            target = target.Substring(0, 3)
            Dim amount As String = AmountTextBox.Text
            If amount = "" Then
                amount = 1
            End If
            Dim pattern As Regex = New Regex("^([0-9]*|\d*\.\d{1}?\d*)$")
            Dim m As Match = pattern.Match(amount)
            If m.Success Then
                amount = m.Groups(1).Value
                AmountTextBox.Text = amount
                Dim requestString As String = "http://www.google.com/ig/calculator?q=" + amount + "%20" + source + "=?" + target
                wc.OpenReadAsync(New Uri(requestString))
            Else
                MessageBox.Show("Invalid amount. Please use a valid decimal number.", "Error", MessageBoxButton.OKCancel)
            End If
        End If
    End Sub

    Private Sub wc_OpenReadCompleted(sender As Object, e As OpenReadCompletedEventArgs)
        ' Set the data context for the listbox to the results.
        Dim totalBytes = 0
        'lhs: "100,9 US-Dollar",rhs: "686,319856 Schwedische Kronen"
        Dim pattern = New Regex("(.*?)rhs: (.*?),(.*?)")
        Dim s As String
        Dim response As System.IO.Stream = CType(e.Result, System.IO.Stream)
        Try
            Dim sr As New System.IO.StreamReader(response, System.Text.Encoding.UTF8)
            Try
                s = sr.ReadToEnd()
                s = s.Replace("'", "")
            Finally
                sr.Close()
            End Try
            Dim m As Match = pattern.Match(s)
            If m.Success Then
                Dim output As String = ""
                output &= m.Groups(2).Value
                If output = """""" Then
                    TextBlock4.Text = "The response could not be parsed."
                Else
                    Dim source As String = FromListBox.SelectedValue
                    source = source.Substring(6).Replace("(", "").Replace(")", "")
                    TextBlock4.Text = AmountTextBox.Text & " " & source & vbCrLf & output.Replace("""", "")
                End If

            Else
                TextBlock4.Text = "The response could not be parsed."
            End If

        Finally
            response.Close()
        End Try
    End Sub

    Private Sub AddItems()
        Dim currencyStrings() As String = { _
            "AED", "ANG", "ARS", "AUD", "BGN", "BHD", "BND", "BOB", "BRL", "BWP", "CAD", "CHF", "CLP", "CNY", "COP", "CRC", _
            "CZK", "DKK", "DOP", "DZD", "EGP", "EUR", "FJD", "GBP", "HKD", "HNL", "HRK", "HUF", "IDR", "ILS", "INR", "JMD", _
            "JOD", "JPY", "KES", "KRW", "KWD", "KYD", "KZT", "LBP", "LKR", "LTL", "LVL", "MAD", "MDL", "MKD", "MUR", "MXN", _
            "MXV", "MYR", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "OMR", "PEN", "PGK", "PHP", "PKR", "PLN", "PYG", "QAR", _
            "RON", "RSD", "RUB", "SAR", "SCR", "SEK", "SGD", "SLL", "SVC", "THB", "TND", "TRY", "TTD", "TWD", "TZS", "UAH", _
            "UGX", "USD", "UYU", "UZS", "VND", "YER", "ZAR", "ZMK"}
        Dim currencyStrings2() As String = { _
            "AED - (United Arab Emirates dirhams)", "ANG - (Netherlands Antilles guilders)", "ARS - (Argentine pesos)", _
            "AUD - (Australian dollars)", "BGN - (Bulgarian levs)", "BHD - (Bahrain dinars)", "BND - (Brunei dollars)", _
            "BOB - (Bolivian bolivianos)", "BRL - (Brazil reais)", "BWP - (Botswana pula)", "CAD - (Canadian dollars)", _
            "CHF - (Swiss francs)", "CLP - (Chilean pesos)", "CNY - (Chinese yuan)", "COP - (Colombian pesos)", "CRC - (Costa Rican colones)", _
            "CZK - (Czech koruny)", "DKK - (Danish kroner)", "DOP - (Dominican pesos)", "DZD - (Algerian dinars)", _
            "EGP - (Egyptian pounds)", "EUR - (Euros)", "FJD - (Fiji dollars)", "GBP - (British pounds)", "HKD - (Hong Kong dollars)", _
            "HNL - (Honduran lempiras)", "HRK - (Croatian kune)", "HUF - (Hungarian forints)", "IDR - (Indonesian rupiahs)", _
            "ILS - (Israeli shekels)", "INR - (Indian rupees)", "JMD - (Jamaican dollars)", "JOD - (Jordanian dinars)", _
            "JPY - (Japanese yen)", "KES - (Kenyan shillings)", "KRW - (South Korean won)", "KWD - (Kuwaiti dinars)", _
            "KYD - (Cayman Islands dollars)", "KZT - (Kazakh tenge)", "LBP - (Lebanese pounds)", "LKR - (Sri Lankan rupees)", _
            "LTL - (Lithuanian litai)", "LVL - (Latvian lati)", "MAD - (Moroccan dirhams)", "MDL - (Moldovan lei)", _
            "MKD - (Macedonian denari)", "MUR - (Mauritian rupees)", "MXN - (Mexican pesos)", "MYR - (Malaysian ringgits)", _
            "NAD - (Namibian dollars)", "NGN - (Nigerian naira)", "NIO - (Nicaraguan cordobas)", "NOK - (Norwegian kroner)", _
            "NPR - (Nepalese rupees)", "NZD - (New Zealand dollars)", "OMR - (Omani rials)", "PEN - (Peruvian nuevos soles)", _
            "PGK - (Papua New Guinean kina)", "PHP - (Philippine pesos)", "PKR - (Pakistan rupees)", "PLN - (Polish zloty)", _
            "PYG - (Paraguayan guaranies)", "QAR - (Qatar riyals)", "RON - (Romanian leu)", "RSD - (Serbian dinars)", _
            "RUB - (Russian rubles)", "SAR - (Saudi riyals)", "SCR - (Seychelles rupees)", "SEK - (Swedish kronor)", _
            "SGD - (Singapore dollars)", "SLL - (Sierra Leonean leones)", "SVC - (Salvadoran colones)", "THB - (Thai baht)", _
            "TND - (Tunisian dinar)", "TRY - (Turkish liras)", "TTD - (Trinidad dollars)", "TWD - (Taiwan dollars)", _
            "TZS - (Tanzanian shillings)", "UAH - (Ukrainian grivnas)", "UGX - (Ugandan shillings)", "USD - (U.S. dollars)", _
            "UYU - (Uruguayan pesos)", "UZS - (Uzbekistani sum)", "VND - (Vietnamese dong)", "YER - (Yemeni rials)", _
            "ZAR - (South African rands)", "ZMK - (Zambia kwacha)"}
        For x = 0 To currencyStrings2.Length - 1
            FromListBox.Items.Add(currencyStrings2(x))
            ToListBox.Items.Add(currencyStrings2(x))
        Next x
    End Sub

    Private Sub Button2_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button2.Click
        MessageBox.Show(CType(TextBox1.Text, Integer) + CType(AmountTextBox.Text, Integer))
    End Sub
End Class
