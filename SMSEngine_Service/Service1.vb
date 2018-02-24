Imports System.IO.Ports
Imports System.Net.Sockets
Imports System.Threading
Imports System.IO
Imports System.Net



Public Class Service1
    'ComPort - ком порт
    'TargetPhone - телефон
    'text - текст направляемый в смс
    Dim ComPort, TargetPhone, text, KillList0 As String
    Dim TCPPort As Integer
    Dim ListenerThread As New Thread(New ThreadStart(AddressOf Listening))
    Dim server As TcpListener

    Protected Overrides Sub OnStart(ByVal args() As String)
        'reading settings
        Try
            ComPort = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\SMSEngine\", "RS-232", Nothing)
            TCPPort = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\SMSEngine\", "TCP", Nothing)
            If ComPort = "" Or TCPPort = Nothing Then
                Dim d0 As Date = Now
                My.Computer.FileSystem.WriteAllText("C:\smsengine.log", d0 & ": Wrong settings parameters, dropping to default. " & vbNewLine, True)
                Try
                    My.Computer.Registry.CurrentUser.CreateSubKey("SMSEngine\")
                    ' Change MyTestKeyValue to This is a test value. 
                    My.Computer.Registry.SetValue("HKEY_CURRENT_USER\SMSEngine\", "RS-232", "Auto")
                    My.Computer.Registry.SetValue("HKEY_CURRENT_USER\SMSEngine\", "TCP", 65534)
                    My.Computer.Registry.SetValue("HKEY_CURRENT_USER\SMSEngine\", "KILL", "HUAWEI Modem 3.5,HUAWEI Modem 3.0")
                    ComPort = "Auto"
                    TCPPort = 65534
                Catch ex As Exception
                    Dim d1 As Date = Now
                    My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", d1 & ": ERROR on writing settings: " & ex.Message & vbNewLine, True)
                End Try
            End If
            Dim d2 As Date = Now
            My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", d2 & ": Settings readed succsessfully: " & ComPort & " " & TCPPort & vbNewLine, True)
        Catch ex As Exception
            Dim d3 As Date = Now
            My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", d3 & ": ERROR on reading settings: " & ex.Message & vbNewLine, True)
        End Try
        ListenerThread.Start()
    End Sub

    Private Sub Listening()
        Try
            '   server = Nothing
            If TCPPort = Nothing Then
                TCPPort = 65534
            End If
            server = New TcpListener(IPAddress.Any, TCPPort) 'Parse("127.0.0.1"), port)
            server.Start()
            'If server.
            Try 'Then
                Dim D As Date = Now
                My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", D & ": SMSEngine server was STARTED! on port: " & TCPPort & vbNewLine, True)
            Catch ex As Exception
            End Try
            ' Buffer for reading data
            Dim bytes(1024) As Byte
            Dim data As String = Nothing
            While True
                ' Perform a blocking call to accept requests.
                ' You could also user server.AcceptSocket() here.
                Dim client As TcpClient = server.AcceptTcpClient()
                ' Get a stream object for reading and writing
                Dim stream As NetworkStream = client.GetStream()
                Dim i As Int32
                ' Loop to receive all the data sent by the client.
                i = stream.Read(bytes, 0, bytes.Length)
                While (i <> 0)
                    Try
                        ' Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.UTF8.GetString(bytes, 0, i)
                        'суём в массив 0-телефон, 1-сообщение; разделитель-&
                        Dim separators As String = "&"
                        Dim incarray() As String = data.Split(separators.ToCharArray)
                        TargetPhone = incarray(0)
                        text = incarray(1)
                        KillThemAll() 'мочим всякие коннект манагеры                        
                        sms()                 'засылаем смс
                        Exit While
                    Catch ex As IOException
                        Dim d As Date = Now
                        My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", d & ": Error on receiving data: " & ex.Message & vbNewLine, True)
                    End Try
                End While
                ' Shutdown and end connection
                client.Close()
            End While
        Catch e As SocketException
            Dim d0 As Date = Now
            My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", d0 & ": Error on listening the socket: " & e.Message & vbNewLine, True)

        Finally
        End Try
    End Sub

    Sub KillThemAll()
        Try 'получаем список процессоф всяких конектманагеров и убиваем их нахрен
            Dim separator As String = ","
            KillList0 = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\SMSEngine\", "KILL", Nothing)
            Dim killlist() As String = KillList0.Split(separator.ToCharArray) ' = {"HUAWEI Modem 3.5", "HUAWEI Modem 3.0"} 'IO.File.ReadAllLines("C:\SMSEngine\kill.list")
            If killlist.Count = 0 Then
                killlist = {"HUAWEI Modem 3.5", "HUAWEI Modem 3.0"}
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\SMSEngine\", "KILL", "HUAWEI Modem 3.5,HUAWEI Modem 3.5")
            End If
            For i = 0 To killlist.Count - 1
                Process.GetProcessesByName(killlist(i))(0).Kill()
            Next
        Catch ex As Exception
            Dim d As Date = Now
            My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", d & ": smsengine server error on killing connect_manager! " & ex.Message & vbNewLine, True)
            Exit Sub
        End Try
    End Sub

    Protected Overrides Sub OnStop()
        ' Добавьте здесь код для завершающих операций перед остановкой службы.
        Try
            Dim D As Date = Now
            My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", D & ": SMSEngine server was STOPPED!" & vbNewLine, True)
        Catch ex As Exception
        End Try
    End Sub

    'отправка смс
    Public Sub sms()
        If ComPort = "Auto" Then
            GetPort()         'автоопределение порта модема
        End If
        Try
            Dim tel, tel1, tel2, tel3 As String
            Dim tellong As Integer
            tel = TargetPhone 'подстановка телефона сюды
            tel1 = tel.Replace("+", "") 'откидывает + заменой на ""
            tellong = Len(tel1)
            If tellong Mod 2 Then 'проверка на чётность с добавлением F
                tel2 = tel1 & "F"
            Else
                tel2 = tel1
            End If
            'намешиваем символы в номере  
            Dim rez As String
            rez = ""
            Dim i As Byte
            For i = 1 To Len(tel2) Step 2
                rez = rez & Mid(tel2, i + 1, 1) & Mid(tel2, i, 1)
            Next i
            tel3 = rez
            '##################преобразование текста в UCS-2###############################         
            ''загоняем аргументы в переменную через пробел                 
            Dim text0() As Byte = System.Text.Encoding.BigEndianUnicode.GetBytes(text)
            Dim text1 As String = BitConverter.ToString(text0).Replace("-", "")
            Dim textlong As String = Len(text1) 'определяем длину фразы
            Dim textlongHEX As String = Hex(textlong) 'перегоняем длину сообщения в HEX
            Dim l As String = 26 + textlong 'два 0 спереди уже откинуто
            Dim l1 = l / 2
            Dim MSG As String = "0001000B91" & tel3 & "0008" & textlongHEX & text1
            Try
                Dim SP As New SerialPort()
                SP.PortName = ComPort
                SP.BaudRate = 9600
                SP.Parity = Parity.None
                SP.StopBits = StopBits.One
                SP.DataBits = 8
                SP.Handshake = Handshake.RequestToSend
                SP.DtrEnable = True
                SP.RtsEnable = True
                SP.Open()
                SP.WriteLine("AT" & Chr(13) & vbCrLf)
                Threading.Thread.Sleep(1000)
                SP.WriteLine("AT+CMGF=0" & Chr(13) & vbCrLf)
                Threading.Thread.Sleep(1000)
                SP.WriteLine("AT+CMGS=" & l1 & vbCrLf)
                Threading.Thread.Sleep(1000)
                SP.WriteLine(MSG & Chr(26) & vbCrLf)
                Threading.Thread.Sleep(1000)
                Dim answer As String = SP.ReadExisting()
                SP.Close()
                'логгирование
                Dim D As Date = Now
                My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", D & ": Начата отправка СМС с текстом: " & vbNewLine & text & vbNewLine & " на номер: " & TargetPhone & vbNewLine & answer, True)
                Exit Sub
            Catch ex As Exception
                Dim D As Date = Now
                My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", D & ": СМС с текстом: " & vbNewLine & text & vbNewLine & " не была послана на номер: " & TargetPhone & " - ошибка открытия порта!" & vbNewLine & ex.Message & vbNewLine, True)
                Exit Sub
            End Try
        Catch ex As Exception
        End Try

    End Sub

    Private Sub GetPort()
        'автоопределения порта модема
        Dim ports() As String = SerialPort.GetPortNames()
        For i = 0 To ports.Count - 1
            Try
                Dim SP As New SerialPort()
                SP.PortName = ports(i) ' comport
                SP.BaudRate = 9600
                SP.Parity = Parity.None
                SP.StopBits = StopBits.One
                SP.DataBits = 8
                SP.Handshake = Handshake.RequestToSend
                SP.DtrEnable = True
                SP.RtsEnable = True
                SP.Open()
                SP.WriteLine("AT" & Chr(13) & vbCrLf)
                Thread.Sleep(500)
                Dim at As String = SP.ReadExisting
                If at <> "" Then
                    ComPort = ports(i)
                End If
                ' Thread.Sleep(1000)
                SP.Close()
                Try
                    Dim D As Date = Now
                    My.Computer.FileSystem.WriteAllText("C:\SMSEngine\smsengine.log", D & ": modem was found on " & ComPort & vbNewLine, True)
                Catch ex As Exception
                End Try
            Catch ex As Exception
            End Try
        Next
    End Sub
End Class
