Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Text

Namespace IPExt
    Class IPExt
        Public Shared Sub Main(ByVal args As String())

            IPExt.EnableFileWatch = True
            IPExt.Load("17monipdb.datx")

            Console.WriteLine(String.Join("\n", IPExt.Find("8.8.8.8")))
            Console.WriteLine(String.Join("\n", IPExt.Find("255.255.255.255")))
            Console.Read()
        End Sub

        Private Shared EnableFileWatch As Boolean = False
        Private Shared offset As UInteger
        Private Shared index As UInteger() = New UInteger(65536) {}
        Private Shared dataBuffer As Byte()
        Private Shared indexBuffer As Byte()
        Private Shared lastModifyTime As Long = 0L
        Private Shared ipFile As String
        Private Shared ReadOnly lock As Object = New Object()

        Public Shared Sub Load(ByVal filename As String)
            ipFile = New FileInfo(filename).FullName
            Load()
            If EnableFileWatch Then
                watch()
            End If
        End Sub

        Public Shared Function Find(ByVal ip As String) As String()
            SyncLock lock
                Dim ips = ip.Split("."c)
                Dim ip_prefix_value = Integer.Parse(ips(0)) * 256 + Integer.Parse(ips(1))
                Dim ip2long_value As Long = BytesToLong(Byte.Parse(ips(0)), Byte.Parse(ips(1)), Byte.Parse(ips(2)), Byte.Parse(ips(3)))
                Dim start = index(ip_prefix_value)
                Dim max_comp_len = offset - 262144 - 4
                Dim index_offset As long = -1
                Dim index_length = -1
                Dim b As Byte = 0
                For start = start * 9 + 262144 To max_comp_len - 1 Step 9
                    If BytesToLong(indexBuffer(start + 0), indexBuffer(start + 1), indexBuffer(start + 2), indexBuffer(start + 3)) > ip2long_value Then
                        index_offset = BytesToLong(b, indexBuffer(start + 6), indexBuffer(start + 5), indexBuffer(start + 4))
                        index_length = (255 And indexBuffer(start + 7) << 8) + indexBuffer(start + 8)
                        Exit For
                    End If
                Next
                Dim areaBytes = New Byte(index_length) {}
                Array.Copy(dataBuffer, offset + CInt(index_offset) - 262144, areaBytes, 0, index_length)
                Return Encoding.UTF8.GetString(areaBytes).Split("\t")
            End SyncLock
        End Function

        Private Shared Sub watch()
            Dim file = New FileInfo(ipFile)
            If (file.DirectoryName = Nothing) Then Return
            Dim watcher = New FileSystemWatcher(file.DirectoryName, file.Name) With {.NotifyFilter = NotifyFilters.LastWrite}
            AddHandler watcher.Changed, AddressOf onChanged
            watcher.EnableRaisingEvents = True
        End Sub

        Private Shared Function onChanged(s, e)
            Dim time = File.GetLastWriteTime(ipFile).Ticks
            If time > lastModifyTime Then
                Load()
            End If
        End Function

        Private Shared Sub Load()
            SyncLock lock
                Dim file = New FileInfo(ipFile)
                lastModifyTime = file.LastWriteTime.Ticks
                Try
                    dataBuffer = New Byte(file.Length) {}
                    Using fin As New FileStream(file.FullName, FileMode.Open, FileAccess.Read)
                        fin.Read(dataBuffer, 0, dataBuffer.Length)
                    End Using

                    Dim indexLength = BytesToLong(dataBuffer(0), dataBuffer(1), dataBuffer(2), dataBuffer(3))
                    indexBuffer = New Byte(indexLength) {}
                    Array.Copy(dataBuffer, 4, indexBuffer, 0, indexLength)
                    offset = CInt(indexLength)

                    For i = 0 To 256 - 1
                        For j = 0 To 256 - 1
                            index(i * 256 + j) = BytesToLong(indexBuffer((i * 256 + j) * 4 + 3), indexBuffer((i * 256 + j) * 4 + 2), indexBuffer((i * 256 + j) * 4 + 1), indexBuffer((i * 256 + j) * 4))
                        Next
                    Next
                Catch ex As Exception
                    Throw ex
                End Try
            End SyncLock
        End Sub

        Private Shared Function BytesToLong(ByVal a As Byte, ByVal b As Byte, ByVal c As Byte, ByVal d As Byte) As UInteger
            Return (CUInt(a) << 24) Or (CUInt(b) << 16) Or (CUInt(c) << 8) Or d
        End Function
    End Class
End Namespace
