Imports System.Text.RegularExpressions
Imports System.Windows.Media.Imaging
Imports System.Windows.Controls
Partial Public Class MainPage
    Inherits PhoneApplicationPage
    Private wc As WebClient

    Dim myDispatcherTimer As System.Windows.Threading.DispatcherTimer = New System.Windows.Threading.DispatcherTimer()
    Dim myAnimation As System.Windows.Threading.DispatcherTimer = New System.Windows.Threading.DispatcherTimer()
    Dim i As Integer = 0
    Dim urls() As String
    ' Constructor
    Public Sub New()
        InitializeComponent()

        wc = New WebClient()
        wc.Encoding = System.Text.Encoding.UTF8

        AddHandler wc.OpenReadCompleted, New OpenReadCompletedEventHandler(AddressOf wc_OpenReadCompleted)
    End Sub

    Private Sub Button1_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles Button1.Click
        getResults(TextBox1.Text)
    End Sub

    Private Sub wc_OpenReadCompleted(sender As Object, e As OpenReadCompletedEventArgs)
        ' Set the data context for the listbox to the results.
        Dim totalBytes = 0


        '"MediaUrl":"http:\/\/support.microsoft.com\/library\/images\/support\/kbgraphics\/Public\/EN-US\/XBOX\/360s\/opticalport360s.jpg"
        Dim pattern As Regex = New Regex("""MediaUrl"":""(.*?)"",", RegexOptions.IgnoreCase)
        Dim s As String
        Dim response As System.IO.Stream = CType(e.Result, System.IO.Stream)
        Try
            Dim sr As New System.IO.StreamReader(response, System.Text.Encoding.UTF8)
            Try
                s = sr.ReadToEnd()
                s = s.Replace("\/", "/")
            Finally
                sr.Close()
            End Try


            ' Match the regular expression pattern against a text string.
            Dim m As Match = pattern.Match(s)
            'Dim urls(m.Length) As String
            Dim matchCount As Integer = 0
            Do While m.Success
                Dim g As Group = m.Groups(1)
                ReDim Preserve urls(matchCount)
                urls(matchCount) = g.ToString()
                matchCount += 1
                m = m.NextMatch()
            Loop
            'urls holds an array of URLs
            StartTimer()
        Finally
            response.Close()
        End Try
    End Sub

    Private Sub getResults(searchterm As String)
        Dim url As String = "http://api.bing.net/json.aspx?AppId=A34B1552C3B3DF826089895CCA0D868F0A81EF9D&Query=" & searchterm & "&Sources=Image&Image.Count=20&Image.Filters=Size:Large"
        wc.OpenReadAsync(New Uri(url))
    End Sub

    Private Sub loadImage(url As String)
        Dim thisImage = New BitmapImage()
        AddHandler thisImage.ImageOpened, New System.EventHandler(Of RoutedEventArgs)(AddressOf onImageOpened)
        AddHandler thisImage.ImageFailed, New System.EventHandler(Of ExceptionRoutedEventArgs)(AddressOf onImageFailed)
        thisImage.UriSource = New Uri(url, UriKind.RelativeOrAbsolute)
        Image2.Source = thisImage
    End Sub

    Public Sub StartTimer()

        myDispatcherTimer.Interval = New TimeSpan(0, 0, 0, 0, 2000)
        ' 2 seconds 
        AddHandler myDispatcherTimer.Tick, AddressOf Me.Each_Tick
        myDispatcherTimer.Start()
    End Sub

    Private Sub stopTimer()
        myDispatcherTimer.Stop()
    End Sub

    Public Sub startAnimation()
        myAnimation.Interval = New TimeSpan(0, 0, 0, 0, 10)
        AddHandler myAnimation.Tick, AddressOf animate
        myAnimation.Start()
    End Sub

    Private Sub animate()
        Dim margin As System.Windows.Thickness = Image1.Margin
        margin.Left -= 1
        Image1.Margin = margin
    End Sub

    ' Raised every 100 miliseconds while the DispatcherTimer is active.
    Public Sub Each_Tick(ByVal o As Object, ByVal sender As EventArgs)
        stopTimer()
        If i < urls.Length - 1 Then
            loadImage(urls(i))
            i += 1
        ElseIf i < urls.Length Then
            loadImage(urls(i))
            i += 1
        Else
            i = 0
        End If
    End Sub

    Private Sub onImageOpened(sender As Object, e As System.EventArgs)
        Image1.Source = Image2.Source
        Image1.Opacity = 1
        Image1.Margin = New Thickness(0)
        startAnimation()
        ContentPanel.Opacity = 0
        TitlePanel.Opacity = 0
        StartTimer()
    End Sub

    Private Sub onImageFailed(sender As Object, e As System.EventArgs)
        If i < urls.Length - 2 Then
            i += 1
        End If
        loadImage(urls(i))
    End Sub
End Class
