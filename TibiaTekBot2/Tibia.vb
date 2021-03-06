'    Copyright (C) 2007 TibiaTek Development Team
'
'    This file is part of TibiaTek Bot.
'
'    TibiaTek Bot is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    TibiaTek Bot is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with TibiaTek Bot. If not, see http://www.gnu.org/licenses/gpl.txt
'    or write to the Free Software Foundation, 59 Temple Place - Suite 330,
'    Boston, MA 02111-1307, USA.

Imports Scripting, System.Runtime.InteropServices, System.Windows.Forms, System.Math, _
    System.Text, System.IO, System.Drawing, System.Threading, System.IO.Pipes

Public NotInheritable Class Tibia
    Implements ITibia

#Region " Windows API Declarations "
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As Int32, ByRef lpBuffer As Int32, ByVal nSize As Int32, ByVal lpNumberOfBytesWritten As Int32) As Long
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As Int32, ByRef lpBuffer As UInt32, ByVal nSize As Int32, ByVal lpNumberOfBytesWritten As Int32) As Long
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As Int32, ByVal lpBuffer() As Byte, ByVal nSize As Int32, ByVal lpNumberOfBytesWritten As Int32) As Long
    Private Declare Function SetWindowText Lib "user32" Alias "SetWindowTextA" (ByVal hwnd As IntPtr, ByVal lpString As String) As Boolean
    Private Declare Function GetWindowPlacement Lib "user32" (ByVal hWnd As IntPtr, ByRef windowPlacement As WindowPlacement) As Boolean
    Private Declare Function GetForegroundWindow Lib "user32" () As Integer
    Private Declare Function GetWindowThreadProcessId Lib "user32" (ByVal hwnd As IntPtr, ByRef procid As Integer) As Integer
    Private Declare Function ShowWindow Lib "user32" (ByVal handle As IntPtr, ByVal nCmd As Int32) As Boolean
    Private Declare Function FlashWindowEx Lib "user32" (ByRef fwi As FlashWInfo) As Int32
    Private Declare Function VirtualProtectEx Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As UIntPtr, ByVal flNewProtect As UInteger, ByRef lpflOldProtect As UInteger) As Boolean
    Private Declare Function SetForegroundWindow Lib "user32" (ByVal hwnd As IntPtr) As Boolean
    Private Declare Function SendMessage Lib "user32" Alias "SendMessageA" (ByVal hWnd As IntPtr, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    Private Declare Function OpenProcess Lib "kernel32" (ByVal dwDesiredAccessas As Int32, ByVal bInheritHandle As Int32, ByVal dwProcId As Int32) As Int32
    Private Declare Function VirtualAllocEx Lib "kernel32" (ByVal hProcess As Int32, ByVal lpAddress As Int32, ByRef dwSize As Int32, ByVal flAllocationType As Int32, ByVal flProtect As Int32) As Int32
    Private Declare Function GetModuleHandle Lib "kernel32" Alias "GetModuleHandleW" (<MarshalAs(UnmanagedType.LPWStr)> ByVal ModuleName As String) As Int32
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As Int32, ByVal lpThreadAttributes As Int32, ByVal dwStackSize As Int32, ByVal lpStartAddress As Int32, ByVal lpParameter As Int32, ByVal dwCreationFlags As Int32, ByRef lpThreadId As Int32) As Int32
    Private Declare Function GetProcAddress Lib "kernel32" (ByVal hModule As Int32, <MarshalAs(UnmanagedType.LPStr)> ByVal lpProcName As String) As Int32
    Private Declare Function VirtualFreeEx Lib "kernel32" (ByVal hProcess As Int32, ByVal lpAddress As Int32, ByVal dwSize As Int32, ByVal dwFreeType As Int32) As Boolean
    Private Declare Function SetWindowPos Lib "user32" (ByVal hWnd As Int32, ByVal hWndInsertAfter As Int32, ByVal X As Int32, ByVal Y As Int32, ByVal CX As Int32, ByVal CY As Int32, ByVal uFlags As UInt32) As Boolean
    Private Declare Function GetDC Lib "user32" (ByVal hWnd As Int32) As IntPtr
    Private Declare Function CreateCompatibleDC Lib "gdi32" (ByVal HDC As IntPtr) As IntPtr
    Private Declare Function CreateCompatibleBitmap Lib "gdi32" (ByVal HDC As IntPtr, ByVal Width As Int32, ByVal Height As Int32) As IntPtr
    Private Declare Function SelectObject Lib "gdi32" (ByVal hdc As IntPtr, ByVal hgdiobj As IntPtr) As IntPtr
    Private Declare Function BitBlt Lib "gdi32" (ByVal hdc As IntPtr, ByVal nXDest As Integer, ByVal nYDest As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer, ByVal hdcSrc As IntPtr, ByVal nXSrc As Integer, ByVal nYSrc As Integer, ByVal dwRop As TernaryRasterOperations) As Boolean
    Private Declare Function DeleteDC Lib "gdi32" (ByVal hdc As IntPtr) As Boolean
    Private Declare Function ReleaseDC Lib "user32" (ByVal hWnd As IntPtr, ByVal hDC As IntPtr) As Integer
#End Region

#Region " Constants "

    Private Const PROCESS_ALL_ACCESS As Int32 = &H1F0FFF
    Private Const PAGE_READWRITE As Int32 = &H4
    Private Const MEM_COMMIT As Int32 = &H1000
    Private Const MEM_RESERVE As Int32 = &H2000
    Private Const MEM_RELEASE As Int32 = &H8000
    Private Const SWP_NOMOVE As Int32 = &H2
    Private Const SWP_NOSIZE As Int32 = &H1
    Private Const HWND_TOPMOST As Int32 = &HFFFFFFFF
    Private Const HWND_NOTOPMOST As Int32 = &HFFFFFFFE

#End Region

#Region " Events "
    Public Event Starting() Implements ITibia.Starting
    Public Event Started() Implements ITibia.Started
    Public Event Closed() Implements ITibia.Closed
    Public Event Connected() Implements ITibia.Connected
    Public Event Disconnected() Implements ITibia.Disconnected
    Public Event CharacterAttacked(ByVal e As Scripting.Events.Events.CharacterAttackedEventArgs) Implements Scripting.ITibia.CharacterAttacked
    Public Event CharacterConditionsChanged(ByVal e As Scripting.Events.Events.CharacterConditionsChangedEventArgs) Implements Scripting.ITibia.CharacterConditionsChanged
    Public Event MessageReceived(ByVal e As Scripting.Events.Events.MessageReceivedEventArgs) Implements ITibia.MessageReceived

#End Region

#Region " Structures "

    <StructLayout(LayoutKind.Sequential)> _
       Private Structure FlashWInfo
        Dim cbSize As UInt32
        Dim hWnd As IntPtr
        Dim dwFlags As FlashWFlags
        Dim uCount As UInt32
        Dim dwTimeout As UInt32

        Sub New(ByVal Handle As IntPtr, ByVal Flags As FlashWFlags, ByVal Count As UInt32, ByVal Timeout As UInt32)
            hWnd = Handle
            cbSize = Convert.ToUInt32(Marshal.SizeOf(GetType(FlashWInfo)))
            dwFlags = Flags
            uCount = Count
            dwTimeout = Timeout
        End Sub
    End Structure

    Private Structure WindowPlacement
        Public Length As UInteger
        Public Flags As UInteger
        Public ShowCmd As ShowState
        Public MinPosition As Point
        Public MaxPosition As Point
        Public NormalPosition As Rectangle
    End Structure

#End Region

#Region " Enumerations "

    Private Enum ShowState As UInteger
        SW_HIDE = 0
        SW_SHOWNORMAL = 1
        SW_SHOWMINIMIZED = 2
        SW_SHOWMAXIMIZED = 3
        SW_SHOWNOACTIVATE = 4
        SW_SHOW = 5
        SW_MINIMIZE = 6
        SW_SHOWMINNOACTIVE = 7
        SW_SHOWNA = 8
        SW_RESTORE = 9
        SW_SHOWDEFAULT = 10
    End Enum

    <Flags()> _
    Private Enum FlashWFlags As UInt32
        FLASHW_STOP = 0
        FLASHW_CAPTION = 1
        FLASHW_TRAY = 2
        FLASHW_TIMER = 4
        FLASHW_TIMERNOFG = 12
    End Enum

    Private Enum TernaryRasterOperations
        SRCCOPY = &HCC0020 ' dest = source
        SRCPAINT = &HEE0086 ' dest = source OR dest
        SRCAND = &H8800C6 ' dest = source AND dest
        SRCINVERT = &H660046 ' dest = source XOR dest
        SRCERASE = &H440328 ' dest = source AND (NOT dest )
        NOTSRCCOPY = &H330008 ' dest = (NOT source)
        NOTSRCERASE = &H1100A6 ' dest = (NOT src) AND (NOT dest)
        MERGECOPY = &HC000CA ' dest = (source AND pattern)
        MERGEPAINT = &HBB0226 ' dest = (NOT source) OR dest
        PATCOPY = &HF00021 ' dest = pattern
        PATPAINT = &HFB0A09 ' dest = DPSnoo
        PATINVERT = &H5A0049 ' dest = pattern XOR dest
        DSTINVERT = &H550009 ' dest = (NOT dest)
        BLACKNESS = &H42 ' dest = BLACK
        WHITENESS = &HFF0062 ' dest = WHITE
    End Enum
#End Region

#Region " Objects/Variables "

    Private WithEvents ClientProcess As Process
    Private _Visible As Boolean = True
    Private _Directory As String
    Private _Filename As String
    Private _Arguments As String
    Private _MapTiles As IMapTiles
    Private _Objects As Objects
    Private _PipeName As String = ""

    Public WithEvents Pipe As Pipe
    Private _CharacterList() As ITibia.CharacterListEntry
#End Region

#Region " Properties "
    Public ReadOnly Property Objects() As Scripting.IObjects Implements Scripting.ITibia.Objects
        Get
            Return _Objects
        End Get
    End Property

    Public ReadOnly Property CharacterWorld() As String Implements ITibia.CharacterWorld
        Get
            Try
                Dim CharacterIndex As Integer = 0, CharacterListBegin As Integer = 0
                Dim _CharacterWorld As String = String.Empty
                ReadMemory(Consts.ptrCharacterSelectionIndex, CharacterIndex, 1)
                ReadMemory(Consts.ptrCharacterListBegin, CharacterListBegin, 4)
                ReadMemory(CharacterListBegin + (CharacterIndex * Consts.CharacterListDist) + Consts.CharacterListWorldOffset, _CharacterWorld)
                Return _CharacterWorld
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterName() As String Implements ITibia.CharacterName
        Get
            If Not IsConnected Then Return String.Empty
            Try
                Return _CharacterList(CharacterListSelectedIndex).CharacterName
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Get
    End Property

    Public WriteOnly Property CharacterList() As ITibia.CharacterListEntry()
        Set(ByVal value As ITibia.CharacterListEntry())
            _CharacterList = value
        End Set
    End Property

    Public ReadOnly Property CharacterListSelectedIndex() As Integer Implements ITibia.CharacterListSelectedIndex
        Get
            If Not IsConnected Then Return 0
            Try
                Dim CharacterIndex As Integer = 0
                ReadMemory(Consts.ptrCharacterSelectionIndex, CharacterIndex, 1)
                Return CharacterIndex
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterList_() As ITibia.CharacterListEntry() Implements ITibia.CharacterList
        Get
            Try
                Return _CharacterList
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterListCurrentEntry() As ITibia.CharacterListEntry Implements ITibia.CharacterListCurrentEntry
        Get
            If Not IsConnected Then Return Nothing
            Try
                Return _CharacterList(CharacterListSelectedIndex)
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterFightingMode() As ITibia.FightingMode Implements ITibia.CharacterFightingMode
        Get
            Dim FightingMode As Integer = 0
            ReadMemory(Consts.ptrFightingMode, FightingMode, 1)
            Return CByte(FightingMode)
        End Get
    End Property

    Public ReadOnly Property CharacterSecureMode() As ITibia.SecureMode Implements ITibia.CharacterSecureMode
        Get
            Dim SecureMode As Integer = 0
            ReadMemory(Consts.ptrSecureMode, SecureMode, 1)
            Return CByte(SecureMode)
        End Get
    End Property

    Public ReadOnly Property CharacterChasingMode() As ITibia.ChasingMode Implements ITibia.CharacterChasingMode
        Get
            Dim ChasingMode As Integer = 0
            ReadMemory(Consts.ptrChasingMode, ChasingMode, 1)
            Return CByte(ChasingMode)
        End Get
    End Property

    Public ReadOnly Property MapTiles() As IMapTiles Implements ITibia.MapTiles
        Get
            Return _MapTiles
        End Get
    End Property

    Public ReadOnly Property CharacterLocation() As ITibia.LocationDefinition Implements ITibia.CharacterLocation
        Get
            Dim Loc As New ITibia.LocationDefinition
            ReadMemory(Consts.ptrCoordX, Loc.X, 2)
            ReadMemory(Consts.ptrCoordY, Loc.Y, 2)
            ReadMemory(Consts.ptrCoordZ, Loc.Z, 1)
            Return Loc
        End Get
    End Property

    Public Property Title() As String Implements ITibia.Title
        Get
            Try
                Return ClientProcess.MainWindowTitle
            Catch Ex As Exception
                ShowError(Ex)
            End Try
            Return "Tibia   "
        End Get
        Set(ByVal NewTitle As String)
            Try
                SetWindowText(WindowHandle(), NewTitle)
            Catch Ex As Exception
                ShowError(Ex)
            End Try
        End Set
    End Property

    Public ReadOnly Property ProcessHandle() As IntPtr Implements Scripting.ITibia.ProcessHandle
        Get
            Try
                If ClientProcess Is Nothing Then Return 0
                Return ClientProcess.Handle
            Catch Ex As Exception
                ShowError(Ex)
            End Try
            Return 0
        End Get
    End Property

    Public ReadOnly Property ProcessID() As IntPtr Implements Scripting.ITibia.ProcessID
        Get
            Try
                Return ClientProcess.Id
            Catch Ex As Exception
                ShowError(Ex)
            End Try
            Return 0
        End Get
    End Property

    Public ReadOnly Property WindowHandle() As IntPtr Implements Scripting.ITibia.WindowHandle
        Get
            Try
                If ClientProcess.MainWindowHandle = 0 Then ClientProcess.Refresh()
                Return ClientProcess.MainWindowHandle
            Catch Ex As Exception
                ShowError(Ex)
            End Try
            Return 0
        End Get
    End Property

    Public ReadOnly Property WindowState() As ITibia.WindowStates Implements Scripting.ITibia.WindowState
        Get
            Try
                If Not _Visible Then
                    Return ITibia.WindowStates.Hidden
                End If
                Dim WP As New Tibia.WindowPlacement
                WP.Length = Convert.ToByte(Marshal.SizeOf(GetType(Tibia.WindowPlacement)))
                If Tibia.GetWindowPlacement(WindowHandle, WP) = False Then
                    Return ITibia.WindowStates.Inactive
                End If
                Select Case WP.ShowCmd
                    Case ShowState.SW_SHOWNORMAL, ShowState.SW_SHOWMAXIMIZED
                        If GetForegroundWindow() = WindowHandle Then
                            Return ITibia.WindowStates.Active
                        Else
                            Return ITibia.WindowStates.Inactive
                        End If
                    Case ShowState.SW_SHOWMINIMIZED
                        Return ITibia.WindowStates.Minimized
                End Select
                Return ITibia.WindowStates.Inactive
            Catch Ex As Exception
                ShowError(Ex)
            End Try
            Return ITibia.WindowStates.Inactive
        End Get
    End Property

    Public ReadOnly Property Directory() As String Implements Scripting.ITibia.Directory
        Get
            Try
                Return _Directory
            Catch Ex As Exception
                ShowError(Ex)
            End Try
            Return String.Empty
        End Get
    End Property

    Public ReadOnly Property Filename() As String Implements Scripting.ITibia.Filename
        Get
            Try
                Return _Filename
            Catch Ex As Exception
                ShowError(Ex)
            End Try
            Return String.Empty
        End Get
    End Property

    Public ReadOnly Property DialogIsOpened() As Boolean Implements Scripting.ITibia.DialogIsOpened
        Get
            Try
                Dim DialogBegin As Integer = 0
                ReadMemory(Consts.ptrDialogBegin, DialogBegin, 4)
                Return DialogBegin > 0
            Catch ex As Exception
                ShowError(Ex)
                Return False
            End Try
        End Get
    End Property

    Public ReadOnly Property DialogSize() As System.Drawing.Size Implements Scripting.ITibia.DialogSize
        Get
            Try
                Dim DialogBegin As Integer = 0
                ReadMemory(Consts.ptrDialogBegin, DialogBegin, 4)
                If DialogBegin = 0 Then
                    Return System.Drawing.Size.Empty
                Else
                    Dim W As Integer = 0
                    Dim H As Integer = 0
                    ReadMemory(DialogBegin + Consts.DialogWidthOffset, W, 4)
                    ReadMemory(DialogBegin + Consts.DialogWidthOffset, H, 4)
                    Return New System.Drawing.Size(W, H)
                End If
            Catch ex As Exception
                ShowError(Ex)
                Return System.Drawing.Size.Empty
            End Try
        End Get
    End Property

    Public ReadOnly Property DialogLocation() As System.Drawing.Point Implements Scripting.ITibia.DialogLocation
        Get
            Try
                Dim DialogBegin As Integer = 0
                ReadMemory(Consts.ptrDialogBegin, DialogBegin, 4)
                If DialogBegin = 0 Then
                    Return System.Drawing.Point.Empty
                Else
                    Dim X As Integer = 0
                    Dim Y As Integer = 0
                    ReadMemory(DialogBegin + Consts.DialogLeftOffset, X, 4)
                    ReadMemory(DialogBegin + Consts.DialogTopOffset, Y, 4)
                    Return New System.Drawing.Point(X, Y)
                End If
            Catch ex As Exception
                ShowError(Ex)
                Return System.Drawing.Point.Empty
            End Try
        End Get
    End Property

    Public ReadOnly Property DialogCaption() As String Implements ITibia.DialogCaption
        Get
            Try
                Dim DialogBegin As Integer = 0
                Dim Caption As String = String.Empty
                ReadMemory(Consts.ptrDialogBegin, DialogBegin, 4)
                If DialogBegin = 0 Then 'no window opened
                    Return String.Empty
                Else
                    ReadMemory(DialogBegin + Consts.DialogCaptionOffset, Caption)
                    Return Caption
                End If
            Catch ex As Exception
                ShowError(Ex)
                Return String.Empty
            End Try
        End Get
    End Property

    Public ReadOnly Property ConnectionState() As ITibia.ConnectionStates Implements Scripting.ITibia.ConnectionState
        Get
            Try
                Dim _IsInGame As Integer = 0
                ReadMemory(Consts.ptrInGame, _IsInGame, 1)
                Return CType(_IsInGame, ITibia.ConnectionStates)
            Catch ex As Exception
                ShowError(Ex)
            End Try
        End Get
    End Property

    Public ReadOnly Property IsConnected() As Boolean Implements Scripting.ITibia.IsConnected
        Get
            Try
                Return ConnectionState = ITibia.ConnectionStates.Connected
            Catch ex As Exception
                ShowError(Ex)
                Return False
            End Try
        End Get
    End Property

    Public WriteOnly Property TopMost() As Boolean Implements ITibia.TopMost
        Set(ByVal value As Boolean)
            Try
                SetWindowPos(WindowHandle, IIf(value, HWND_TOPMOST, HWND_NOTOPMOST), 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE)
            Catch ex As Exception
                ShowError(Ex)
            End Try
        End Set
    End Property

    Public ReadOnly Property CharacterHasCondition(ByVal Condition As Scripting.ITibia.Conditions) As Boolean Implements ITibia.CharacterHasCondition
        Get
            Try
                Dim CurrentConditions As Integer = 0

                ReadMemory(Consts.ptrConditions, CurrentConditions, 4)
                If (CurrentConditions And Condition) = Condition Then
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterHitPoints() As Integer Implements ITibia.CharacterHitPoints
        Get
            Try
                Dim Hp As Integer
                ReadMemory(Consts.ptrHitPoints, Hp, 4)
                Return Hp
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterManaPoints() As Integer Implements ITibia.CharacterManaPoints
        Get
            Try
                Dim Mp As Integer
                ReadMemory(Consts.ptrManaPoints, Mp, 4)
                Return Mp
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterExperience() As Integer Implements ITibia.CharacterExperience
        Get
            Try
                Dim Exp As Integer
                ReadMemory(Consts.ptrExperience, Exp, 4)
                Return Exp
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    'Not added to the scripting yet!
    Public ReadOnly Property CharacterCapacity() As Integer Implements ITibia.CharacterCapacity
        Get
            Try
                Dim Cap As Integer
                ReadMemory(Consts.ptrCapacity, Cap, 4)
                Return Cap
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterSoulPoints() As Integer Implements ITibia.CharacterSoulPoints
        Get
            Try
                Dim Soul As Integer
                ReadMemory(Consts.ptrSoulPoints, Soul, 4)
                Return Soul
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterStamina() As Integer Implements ITibia.CharacterStamina
        Get
            Try
                Dim Stamina As Integer
                ReadMemory(Consts.ptrStamina, Stamina, 4)
                Return Stamina
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterSkill(ByVal Skill As ITibia.Skills) As Integer Implements ITibia.CharacterSkill
        Get
            Try
                Dim SkillValue As Integer
                ReadMemory(Consts.ptrSkillsBegin + (Skill * Consts.SkillsDist), SkillValue, 1)
                Return SkillValue
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterSkillPercent(ByVal Skill As ITibia.Skills) As Integer Implements ITibia.CharacterSkillPercent
        Get
            Try
                Dim SkillPercent As Integer
                ReadMemory(Consts.ptrSkillsPercentBegin + (Skill * Consts.SkillsDist), SkillPercent, 1)
                Return SkillPercent
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterMagicLevel() As Integer Implements ITibia.CharacterMagicLevel
        Get
            Try
                Dim MLevel As Integer
                ReadMemory(Consts.ptrMagicLevel, MLevel, 1)
                Return MLevel
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property CharacterLevel() As Integer Implements ITibia.CharacterLevel
        Get
            Try
                Dim Level As Integer
                ReadMemory(Consts.ptrLevel, Level, 1)
                Return Level
            Catch ex As Exception
                ShowError(Ex)
                Return Nothing
            End Try
        End Get
    End Property

    Public ReadOnly Property GraphicsEngine() As Scripting.ITibia.GraphicsEngines Implements Scripting.ITibia.GraphicsEngine
        Get
            Try
                Dim GE As Integer = 0
                ReadMemory(Consts.ptrScreenInfoGraphicsEngine, GE, 1)
                Return CType(GE, ITibia.GraphicsEngines)
            Catch ex As Exception
                ShowError(Ex)
                Return ITibia.GraphicsEngines.DirectX5 'default?
            End Try
        End Get
    End Property

    Public ReadOnly Property ScreenBottom() As Integer Implements Scripting.ITibia.ScreenBottom
        Get
            Try
                Dim Result As Integer = 0
                Dim ScreenInfoBegin As Integer = 0
                ReadMemory(Consts.ptrScreenInfoBegin, ScreenInfoBegin, 4)
                Select Case GraphicsEngine
                    Case ITibia.GraphicsEngines.DirectX5
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX5BottomOffset, Result, 4)
                    Case ITibia.GraphicsEngines.OpenGL
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoOGLBottomOffset, Result, 4)
                    Case ITibia.GraphicsEngines.DirectX9
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX9BottomOffset, Result, 4)
                End Select
                Return Result
            Catch ex As Exception
                ShowError(Ex)
                Return 0
            End Try
        End Get
    End Property

    Public ReadOnly Property ScreenHeight() As Integer Implements Scripting.ITibia.ScreenHeight
        Get
            Try
                Dim Result As Integer = 0
                Dim ScreenInfoBegin As Integer = 0
                ReadMemory(Consts.ptrScreenInfoBegin, ScreenInfoBegin, 4)
                Select Case GraphicsEngine
                    Case ITibia.GraphicsEngines.DirectX5
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX5HeightOffset, Result, 4)
                    Case ITibia.GraphicsEngines.OpenGL
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoOGLHeightOffset, Result, 4)
                    Case ITibia.GraphicsEngines.DirectX9
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX9HeightOffset, Result, 4)
                End Select
                Return Result
            Catch ex As Exception
                ShowError(Ex)
                Return 0
            End Try
        End Get
    End Property

    Public ReadOnly Property ScreenLeft() As Integer Implements Scripting.ITibia.ScreenLeft
        Get
            Try
                Dim Result As Integer = 0
                Dim ScreenInfoBegin As Integer = 0
                ReadMemory(Consts.ptrScreenInfoBegin, ScreenInfoBegin, 4)
                Select Case GraphicsEngine
                    Case ITibia.GraphicsEngines.DirectX5
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX5LeftOffset, Result, 4)
                    Case ITibia.GraphicsEngines.OpenGL
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoOGLLeftOffset, Result, 4)
                    Case ITibia.GraphicsEngines.DirectX9
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX9LeftOffset, Result, 4)
                End Select
                Return Result
            Catch ex As Exception
                ShowError(Ex)
                Return 0
            End Try
        End Get
    End Property

    Public ReadOnly Property ScreenRight() As Integer Implements Scripting.ITibia.ScreenRight
        Get
            Try
                Dim Result As Integer = 0
                Dim ScreenInfoBegin As Integer = 0
                ReadMemory(Consts.ptrScreenInfoBegin, ScreenInfoBegin, 4)
                Select Case GraphicsEngine
                    Case ITibia.GraphicsEngines.DirectX5
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX5RightOffset, Result, 4)
                    Case ITibia.GraphicsEngines.OpenGL
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoOGLRightOffset, Result, 4)
                    Case ITibia.GraphicsEngines.DirectX9
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX9RightOffset, Result, 4)
                End Select
                Return Result
            Catch ex As Exception
                ShowError(Ex)
                Return 0
            End Try
        End Get
    End Property

    Public ReadOnly Property ScreenTop() As Integer Implements Scripting.ITibia.ScreenTop
        Get
            Try
                Dim Result As Integer = 0
                Dim ScreenInfoBegin As Integer = 0
                ReadMemory(Consts.ptrScreenInfoBegin, ScreenInfoBegin, 4)
                Select Case GraphicsEngine
                    Case ITibia.GraphicsEngines.DirectX5
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX5TopOffset, Result, 4)
                    Case ITibia.GraphicsEngines.OpenGL
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoOGLTopOffset, Result, 4)
                    Case ITibia.GraphicsEngines.DirectX9
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX9TopOffset, Result, 4)
                End Select
                Return Result
            Catch ex As Exception
                ShowError(Ex)
                Return 0
            End Try
        End Get
    End Property

    Public ReadOnly Property ScreenWidth() As Integer Implements Scripting.ITibia.ScreenWidth
        Get
            Try
                Dim Result As Integer = 0
                Dim ScreenInfoBegin As Integer = 0
                ReadMemory(Consts.ptrScreenInfoBegin, ScreenInfoBegin, 4)
                Select Case GraphicsEngine
                    Case ITibia.GraphicsEngines.DirectX5
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX5WidthOffset, Result, 4)
                    Case ITibia.GraphicsEngines.OpenGL
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoOGLWidthOffset, Result, 4)
                    Case ITibia.GraphicsEngines.DirectX9
                        ReadMemory(ScreenInfoBegin + Consts.ScreenInfoDX9WidthOffset, Result, 4)
                End Select
                Return Result
            Catch ex As Exception
                ShowError(Ex)
                Return 0
            End Try
        End Get
    End Property

    Public ReadOnly Property GetStatusText() As String Implements ITibia.GetStatusText
        Get
            Try
                Dim StatusText As String = ""
                Kernel.Client.ReadMemory(Consts.ptrStatusMessage, StatusText)
                Return StatusText
            Catch ex As Exception
                ShowError(ex)
                Return ""
            End Try
        End Get
    End Property

    Public ReadOnly Property GetStatusTimer() As Integer Implements ITibia.GetStatusTimer
        Get
            Try
                Dim StatusTimer As Integer = 0
                Kernel.Client.ReadMemory(Consts.ptrStatusMessage, StatusTimer, 4)
                Return StatusTimer
            Catch ex As Exception
                ShowError(ex)
                Return 0
            End Try

        End Get
    End Property



#End Region

#Region " Methods "

    Public Sub New(ByVal Filename As String, ByVal Directory As String, Optional ByVal Arguments As String = "")
        Try
            _Filename = Filename
            _Directory = Directory
            _Arguments = Arguments
            _MapTiles = New MapTiles
            _Objects = New Objects(Me)


            Dim PipeNum As Integer = (New Random((System.DateTime.Now.Hour * 1000 * 60) + (System.DateTime.Now.Minute * 1000) + System.DateTime.Now.Millisecond)).Next(10000, 100000)
            _PipeName = "ttb" & PipeNum
            Pipe = New Pipe(_PipeName)

            '_Pipe = New NamedPipeServerStream(_PipeName, Pipes.PipeDirection.InOut)
        Catch Ex As Exception
            ShowError(Ex)
            End
        End Try
    End Sub

    Public Sub Start()
        Try
            Dim PSI As New Diagnostics.ProcessStartInfo()
            PSI.FileName = _Filename
            PSI.WorkingDirectory = _Directory
            PSI.UseShellExecute = True
            'RaiseEvent CharacterAttacked(
            PSI.Arguments = _Arguments & " -pipe:" & _PipeName.Substring(3)
            ClientProcess = Process.Start(PSI)
            RaiseEvent Starting()
            ClientProcess.EnableRaisingEvents = True
            ClientProcess.Refresh()
            ClientProcess.WaitForInputIdle()
            ClientProcess.Refresh()
            RaiseEvent Started()
            Dim FVI As FileVersionInfo = FileVersionInfo.GetVersionInfo(_Directory & "\" & _Filename)
            Kernel.CurrentTibiaVersion = FVI.FileVersion
        Catch Ex As Exception
            ShowError(Ex)
            End
        End Try
    End Sub

    'Private Sub Pipe_OnRead(ByVal Buffer() As Byte) Handles Pipe.OnReceive
    '    MsgBox(System.Text.ASCIIEncoding.ASCII.GetString(Buffer))
    'End Sub

    Sub TestPipe()
        Try
            Dim PPB As New PipePacketBuilder(Kernel.Client.Pipe)
            PPB.Test()
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Private Sub Pipe_OnConnected() Handles Pipe.OnConnected
        Try
            'Send Constants First!
            Dim PPB As New PipePacketBuilder(Pipe, False)
            PPB.SetConstant("ptrCharX", Consts.ptrCoordX)
            PPB.SetConstant("ptrCharY", Consts.ptrCoordY)
            PPB.SetConstant("ptrCharZ", Consts.ptrCoordZ)

            PPB.SetConstant("ptrInGame", Consts.ptrInGame)
            PPB.SetConstant("ptrWASDPopup", Consts.ptrWASDPopup)
            PPB.SetConstant("TibiaWindowHandle", WindowHandle)

            PPB.SetConstant("ptrPrintName", Consts.ptrPrintName)
            PPB.SetConstant("ptrPrintFPS", Consts.ptrPrintFPS)
            PPB.SetConstant("ptrShowFPS", Consts.ptrShowFPS)
            PPB.SetConstant("ptrPrintTextFunc", Consts.ptrPrintTextFunc)

            PPB.SetConstant("ptrBattlelistBegin", Consts.ptrBattleListBegin)
            PPB.SetConstant("BLMax", Consts.BLMax)
            PPB.SetConstant("BLDist", Consts.BLDist)
            PPB.SetConstant("BLNameOffset", Consts.BLNameOffset)
            PPB.SetConstant("BLLocationOffset", Consts.BLCoordXOffset)
            PPB.SetConstant("BLOnScreenOffset", Consts.BLOnScreenOffset)
            PPB.SetConstant("BLHPPercentOffset", Consts.BLHPPercentOffset)
            PPB.SetConstant("ptrNopFPS", Consts.ptrNopFPS)
            PPB.SetConstant("ptrCharacterID", Consts.ptrCharacterID)

            PPB.HookWndProc(True)
            PPB.Send()
            PPB.InjectDisplay(True)
            PPB.Send()
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Function InjectDLL(ByVal Filename As String) As Boolean
        Try
            Dim hProcess As Int32 = ProcessHandle
            Dim lpRemoteAddress As Int32 = AllocateMemory(Filename.Length)
            WriteMemory(lpRemoteAddress, Filename)
            Dim hThread As Int32 = CreateRemoteThread(hProcess, 0, 0, GetProcAddress(GetModuleHandle("Kernel32"), "LoadLibraryA"), lpRemoteAddress, 0, 0)
            DeallocateMemory(lpRemoteAddress, Filename.Length)
            Return (hThread > 0 AndAlso lpRemoteAddress > 0 AndAlso hProcess > 0)
        Catch Ex As Exception
            ShowError(Ex)
            Return False
        End Try
    End Function

    Private Sub ClientProcess_Closed() Handles ClientProcess.Exited
        RaiseEvent Closed()
    End Sub

    Public Sub Minimize() Implements Scripting.ITibia.Minimize
        Try
            ShowWindow(WindowHandle, ShowState.SW_MINIMIZE)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Restore() Implements Scripting.ITibia.Restore
        Try
            ShowWindow(WindowHandle, ShowState.SW_RESTORE)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Maximize() Implements Scripting.ITibia.Maximize
        Try
            ShowWindow(WindowHandle, ShowState.SW_SHOWMAXIMIZED)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Show() Implements Scripting.ITibia.Show
        Try
            _Visible = True
            ShowWindow(WindowHandle, ShowState.SW_SHOW)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Hide() Implements Scripting.ITibia.Hide
        Try
            _Visible = False
            ShowWindow(WindowHandle, ShowState.SW_HIDE)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Close() Implements Scripting.ITibia.Close
        Try
            ClientProcess.Kill()
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Activate() Implements Scripting.ITibia.Activate
        Try
            SetForegroundWindow(WindowHandle)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ConsoleError(ByVal Message As String) Implements Scripting.ITibia.ConsoleError
        Try
            Kernel.ConsoleError(Message)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ConsoleRead(ByVal Message As String) Implements Scripting.ITibia.ConsoleRead
        Try
            Kernel.ConsoleRead(Message)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ConsoleWrite(ByVal Message As String) Implements Scripting.ITibia.ConsoleWrite
        Try
            Kernel.ConsoleWrite(Message)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub FlashWindow(Optional ByVal [Stop] As Boolean = False) Implements Scripting.ITibia.FlashWindow
        Try
            If [Stop] Then
                Dim FWI As New FlashWInfo(WindowHandle, FlashWFlags.FLASHW_STOP, 0, 0)
                FlashWindowEx(FWI)
            Else
                Dim FWI As New FlashWInfo(WindowHandle, FlashWFlags.FLASHW_TIMERNOFG Or FlashWFlags.FLASHW_TRAY Or FlashWFlags.FLASHW_CAPTION, 0, 0)
                FlashWindowEx(FWI)
            End If
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Function AllocateMemory(ByVal Length As Integer) As Integer
        Try
            Return VirtualAllocEx(OpenProcess(PROCESS_ALL_ACCESS, False, ProcessID), 0, Length, MEM_COMMIT Or MEM_RESERVE, PAGE_READWRITE)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
        Return 0
    End Function

    Public Function DeallocateMemory(ByVal Address As Integer, ByVal Length As Integer) As Integer
        Try
            Return VirtualFreeEx(OpenProcess(PROCESS_ALL_ACCESS, False, ProcessID), Address, Length, MEM_RELEASE)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
        Return 0
    End Function

    Public Sub UnprotectMemory(ByVal Address As Integer, ByVal Length As Integer) Implements ITibia.UnprotectMemory
        Try
            Dim Temp As UInteger = 0
            VirtualProtectEx(ProcessHandle, Address, Length, &H40, Temp)
        Catch Ex As Exception
            ShowError(Ex)
            End
        End Try
    End Sub

    Public Sub ProtectMemory(ByVal Address As Integer, ByVal Length As Integer) Implements ITibia.ProtectMemory
        Try
            Dim Temp As UInteger = 0
            VirtualProtectEx(ProcessHandle, Address, Length, &H2, Temp)
        Catch Ex As Exception
            ShowError(Ex)
            End
        End Try
    End Sub

    Public Function ShowMessage(ByVal Message As String, Optional ByVal Caption As String = "", Optional ByVal Buttons As System.Windows.Forms.MessageBoxButtons = System.Windows.Forms.MessageBoxButtons.OK, Optional ByVal Icon As System.Windows.Forms.MessageBoxIcon = System.Windows.Forms.MessageBoxIcon.None, Optional ByVal DefaultButton As System.Windows.Forms.MessageBoxDefaultButton = System.Windows.Forms.MessageBoxDefaultButton.Button1, Optional ByVal Options As System.Windows.Forms.MessageBoxOptions = System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly) As DialogResult Implements Scripting.ITibia.MessageBox
        Return MessageBox.Show(Message, Caption, Buttons, Icon, DefaultButton, Options)
    End Function

    Public Function SendMessage(ByVal MessageID As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer Implements Scripting.ITibia.SendMessage
        SendMessage(WindowHandle, MessageID, wParam, lParam)
    End Function

    Public Function BringToFront() As Boolean Implements ITibia.BringToFront
        Return SetForegroundWindow(WindowHandle)
    End Function

    Public Sub WriteMemory(ByVal Address As Integer, ByVal Value As Integer, ByVal Size As Integer)
        Try
            Dim bytArray() As Byte
            bytArray = BitConverter.GetBytes(Value)
            WriteProcessMemory(ProcessHandle, Address, bytArray, Size, 0)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub WriteMemory(ByVal Address As Integer, ByVal Value() As Byte)
        Try
            WriteProcessMemory(ProcessHandle, Address, Value, Value.Length, 0)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub WriteMemory(ByVal Address As Integer, ByVal Value() As Byte, ByVal Offset As Integer, ByVal Length As Integer)
        Try
            Dim Count1 As Integer
            For Count1 = 0 To Length - 1
                WriteMemory(Address + Count1, Value(Count1 + Offset), 1)
            Next
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub WriteMemory(ByVal Address As Integer, ByVal Value As String)
        Try
            Dim Length As Integer = Value.Length
            For I As Integer = 0 To Length - 1
                WriteMemory(Address + I, Asc(Value.Chars(I)), 1)
            Next
            WriteMemory(Address + Length, 0, 1)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub WriteMemory(ByVal Address As Integer, ByVal Value As Double)
        Try
            Dim Buffer(0 To 7) As Byte
            Buffer = BitConverter.GetBytes(Value)
            For I As Integer = 0 To 7
                WriteMemory(Address + I, CInt(Buffer(I)), 1)
            Next
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ReadMemory(ByVal Address As Integer, ByRef Value As Double)
        Try
            Dim Buffer(7) As Byte
            Dim Temp As Integer
            For I As Integer = 0 To 7
                ReadMemory(Address + I, Temp, 1)
                Buffer(I) = CByte(Temp)
            Next
            Value = BitConverter.ToDouble(Buffer, 0)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ReadMemory(ByVal Address As Integer, ByRef Value As Integer, ByVal Size As Integer)
        Try
            ReadProcessMemory(ProcessHandle, Address, Value, Size, 0)
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ReadMemory(ByVal Address As Integer, ByRef Value As UInteger, ByVal Size As Integer)
        Try
            Dim mValue As UInteger = 0
            ReadProcessMemory(ProcessHandle, Address, mValue, Size, 0)
            Value = mValue
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ReadMemory(ByVal Address As Integer, ByRef Value() As Byte, ByVal Length As Integer)
        Try
            Dim bytArray() As Byte
            Dim Count1 As Integer
            Dim tempInteger As Integer
            ReDim bytArray(Length - 1)
            For Count1 = 0 To Length - 1
                ReadMemory(Address + Count1, tempInteger, 1)
                bytArray(Count1) = CByte(tempInteger)
            Next
            Value = bytArray
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ReadMemory(ByVal Address As Integer, ByRef Value As String)
        Try
            Dim intChar As Integer
            Dim Count1 As Integer
            Dim strTemp As String
            strTemp = String.Empty
            Count1 = 0
            Do
                ReadMemory(Address + Count1, intChar, 1)
                If intChar <> 0 Then strTemp = strTemp & Chr(intChar)
                Count1 += 1
            Loop Until intChar = 0
            Value = strTemp
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub ReadMemory(ByVal Address As Integer, ByRef Value As String, ByVal Length As Integer)
        Try
            Dim intChar As Integer
            Dim Count1 As Integer
            Dim strTemp As String
            strTemp = String.Empty
            For Count1 = 0 To Length - 1
                ReadMemory(Address + Count1, intChar, 1)
                strTemp = strTemp & Chr(intChar)
            Next
            Value = strTemp
        Catch Ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub SetFramesPerSecond(ByVal FPS As Double) Implements Scripting.ITibia.SetFramesPerSecond
        Try
            If FPS <= 0 Then FPS = 1
            Dim NewFPS As Double = 1000.0 / FPS
            Dim FrameRateBegin As Integer = 0
            ReadMemory(Consts.ptrScreenInfoBegin, FrameRateBegin, 4)
            WriteMemory(FrameRateBegin + Consts.ScreenInfoFrameRateLimitOffset, NewFPS)
        Catch ex As Exception
            ShowError(ex)
        End Try
    End Sub

    Public Sub CharacterMove(ByVal Location As ITibia.LocationDefinition) Implements Scripting.ITibia.CharacterMove
        Try
            Dim BL As New BattleList
            WriteMemory(Consts.ptrGoToX, Location.X, 2)
            WriteMemory(Consts.ptrGoToY, Location.Y, 2)
            WriteMemory(Consts.ptrGoToZ, Location.Z, 1)
            BL.IsWalking = True
        Catch ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Click(ByVal X As Integer, ByVal Y As Integer) Implements Scripting.ITibia.Click
        Try
            SendMessage(&H202, 0, 0)
            SendMessage(&H201, 0, (Y << &H10) + (X And &HFFFF))
            SendMessage(&H202, 0, (Y << &H10) + (X And &HFFFF))
        Catch ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Sub Click(ByVal Point As System.Drawing.Point) Implements Scripting.ITibia.Click
        Try
            Click(Point.X, Point.Y)
        Catch ex As Exception
            ShowError(Ex)
        End Try
    End Sub

    Public Function Screenshot(Optional ByVal BringToTop As Boolean = True) As System.Drawing.Image Implements Scripting.ITibia.Screenshot
        Try
            If BringToTop Then
                If WindowState = ITibia.WindowStates.Minimized Then
                    Restore()
                    System.Threading.Thread.Sleep(300)
                End If
                BringToFront()
                TopMost = True
            End If
            Dim DesktopDeviceContext As IntPtr = 0
            Dim NewDeviceContext As IntPtr = 0
            Dim NewObject As IntPtr = 0
            Dim OldObject As IntPtr = 0
            DesktopDeviceContext = GetDC(WindowHandle)
            NewDeviceContext = CreateCompatibleDC(DesktopDeviceContext)
            NewObject = CreateCompatibleBitmap(DesktopDeviceContext, ScreenWidth, ScreenHeight)
            If NewObject <> IntPtr.Zero Then
                OldObject = SelectObject(NewDeviceContext, NewObject)
                BitBlt(NewDeviceContext, 0, 0, ScreenWidth, ScreenHeight, DesktopDeviceContext, 0, 0, TernaryRasterOperations.SRCCOPY)
                SelectObject(NewDeviceContext, OldObject)
                DeleteDC(NewDeviceContext)
                ReleaseDC(WindowHandle, DesktopDeviceContext)
                Return Image.FromHbitmap(NewObject)
            Else
                Throw New Exception("CreateCompatibleBitmap failed.")
            End If
            If BringToTop Then
                TopMost = False
            End If
        Catch ex As Exception
            ShowError(Ex)
            Return Nothing
        End Try
    End Function

#Region " Event Raising"

    Public Sub [RaiseEvent](ByVal Kind As ITibia.EventKind, ByVal e As EventArgs)
        Try
            Select Case Kind
                Case ITibia.EventKind.CharacterAttacked
                    RaiseEvent CharacterAttacked(e)
                Case ITibia.EventKind.CharacterConditionsChanged
                    RaiseEvent CharacterConditionsChanged(e)
                Case ITibia.EventKind.MessageReceived
                    RaiseEvent MessageReceived(e)
            End Select
        Catch ex As Exception
            ShowError(Ex)
        End Try
    End Sub

#End Region

#End Region


End Class