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

Imports Scripting

Public Module ContainerModule

    Public Class Container
        Implements IContainer

        Private ContainerIndex As Integer = 0
        Private ContainerItemCount As Integer = 0
        Private ContainerIsOpened As Boolean = False

        Public Sub New()
            ContainerIndex = 0
        End Sub

        Public Function Reset() As Boolean Implements IContainer.Reset
            Try
                ContainerIndex = 0
                Dim IsOpened As Integer = 0
                Kernel.Client.ReadMemory(Consts.ptrFirstContainer, IsOpened, 1)
                If CBool(IsOpened) Then
                    ContainerItemCount = Me.GetItemCount()
                    Me.ContainerIsOpened = True
                End If
                Return Me.ContainerIsOpened
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public Function JumpToContainer(ByVal NewContainerIndex As Integer) As Boolean Implements IContainer.JumpToContainer
            Try
                If NewContainerIndex > Consts.MaxContainers Then NewContainerIndex = Consts.MaxContainers
                If Container.IsOpened(NewContainerIndex) Then
                    Me.ContainerIndex = NewContainerIndex
                    ContainerItemCount = GetItemCount()
                    ContainerIsOpened = True
                    Return True
                Else
                    Return False
                End If
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public Function FindItem(ByRef Item As IContainer.ContainerItemDefinition, ByVal ItemID As Integer, Optional ByVal ContainerIndexOffset As Integer = 0, Optional ByVal IndexOffset As Integer = 0, Optional ByVal ContainerIndexMax As Integer = 0, Optional ByVal MinCount As Integer = 0, Optional ByVal MaxCount As Integer = 100) As Boolean Implements IContainer.FindItem
            Try
                Dim mIsOpened As Integer = 0
                Dim mContainerItemCount As Integer = 0
                Dim mItemID As Integer = 0
                Dim mItemCount As Integer = 0
                Dim FirstLoop As Boolean = True
                If ContainerIndexMax = 0 Then ContainerIndexMax = ContainerIndexOffset
                If ContainerIndexMax >= Consts.MaxContainers Then ContainerIndexMax = Consts.MaxContainers - 1
                If ContainerIndexOffset > (Consts.MaxContainers - 1) Then ContainerIndexOffset = Consts.MaxContainers - 1
                For I As Integer = ContainerIndexOffset To ContainerIndexMax
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (I * Consts.ContainerDist), mIsOpened, 1)
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (I * Consts.ContainerDist) + Consts.ContainerItemCountOffset, mContainerItemCount, 1)
                    If CBool(mIsOpened) Then
                        Dim ItemIndexStart As Integer
                        If FirstLoop Then
                            ItemIndexStart = IndexOffset
                            FirstLoop = False
                        Else
                            ItemIndexStart = 0
                        End If
                        If ItemIndexStart >= mContainerItemCount Then Continue For
                        For E As Integer = ItemIndexStart To mContainerItemCount - 1
                            Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (I * Consts.ContainerDist) + (Consts.ItemDist * E) + Consts.ContainerFirstItemOffset, mItemID, 2)
                            Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (I * Consts.ContainerDist) + (Consts.ItemDist * E) + Consts.ContainerFirstItemOffset + Consts.ItemCountOffset, mItemCount, 1)
                            If ItemID = mItemID AndAlso mItemCount >= MinCount AndAlso mItemCount <= MaxCount Then 'found!
                                Item.ID = ItemID
                                Item.Count = mItemCount
                                Item.ContainerIndex = I
                                Item.Slot = E
                                Return True
                            End If
                        Next
                    End If
                Next
                Return False
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public Function NextContainer() As Boolean Implements IContainer.NextContainer
            Try
                Dim mIsOpened As Integer = 0
                For I As Integer = Me.ContainerIndex + 1 To Consts.MaxContainers - 1
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (I * Consts.ContainerDist), mIsOpened, 1)
                    If CBool(mIsOpened) Then
                        ContainerIndex = I
                        ContainerItemCount = GetItemCount()
                        ContainerIsOpened = True
                        Return True
                    End If
                Next
                Return False
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function
        Public Function ContainerCount() As Integer Implements IContainer.ContainerCount
            Try
                Dim ContCount As Integer = 0
                Dim Cont As New Container
                Cont.Reset()
                Do
                    If Cont.IsOpened() Then
                        ContCount += 1
                    End If
                Loop While Cont.NextContainer()
                Return ContCount
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public ReadOnly Property GetBackpackCount() As Integer
            Get
                Try
                    Dim ContCount As Integer = 0
                    Dim Cont As New Container
                    Cont.Reset()
                    Do
                        If Cont.IsOpened() AndAlso Not String.IsNullOrEmpty(Kernel.Client.Objects.Name(Cont.GetContainerID)) Then
                            ContCount += 1
                        End If
                    Loop While Cont.NextContainer()
                    Return ContCount
                Catch ex As Exception
                    ShowError(ex)
                End Try
            End Get
        End Property

        Public Shared Function IsOpened(ByVal Index As Integer) As Boolean
            Try
                Dim mIsOpened As Integer = 0
                Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (Index * Consts.ContainerDist), mIsOpened, 1)
                Return CBool(mIsOpened)
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public Function IsOpened() As Boolean Implements IContainer.IsOpened
            Try
                Dim mIsOpened As Integer = 0
                Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist), mIsOpened, 1)
                Return CBool(mIsOpened)
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public ReadOnly Property GetName() As String Implements IContainer.GetName
            Get
                Try
                    Dim Name As String = ""
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist) + Consts.ContainerNameOffset, Name)
                    Return Name
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public Function PrevContainer() As Boolean Implements IContainer.PrevContainer
            Try
                Dim mIsOpened As Integer = 0
                For I As Integer = ContainerIndex To 0 Step -1
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (I * Consts.ContainerDist), mIsOpened, 1)
                    If CBool(mIsOpened) Then
                        ContainerIndex = I
                        Me.ContainerItemCount = GetItemCount()
                        Me.ContainerIsOpened = True
                        Return True
                    End If
                Next
                Return False
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public Function GetItemCountByItemID(ByVal ItemID As UShort) As Integer Implements IContainer.GetItemCountByItemID
            Try
                Dim Item As IContainer.ContainerItemDefinition
                Dim Count As Integer = 0
                Dim ContainerItemCount As Integer
                Dim MyC As New Container
                MyC.Reset()
                Do
                    If MyC.IsOpened() Then
                        ContainerItemCount = MyC.GetItemCount
                        For I As Integer = 0 To ContainerItemCount - 1
                            Item = MyC.Items(I)
                            If Item.ID = ItemID Then
                                If Item.Count = 0 Then
                                    Count += 1
                                Else
                                    Count += Item.Count
                                End If
                            End If
                        Next
                    End If
                Loop While MyC.NextContainer()
                Return Count
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public ReadOnly Property GetItemCount() As Integer Implements IContainer.GetItemCount
            Get
                Try
                    Dim ItemCount As Integer = 0
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist) + Consts.ContainerItemCountOffset, ItemCount, 1)
                    Return CInt(ItemCount)
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetContainerSize() As Integer Implements IContainer.GetContainerSize
            Get
                Try
                    Dim Size As Integer = 0
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist) + Consts.ContainerSizeOffset, Size, 1)
                    Return CInt(Size)
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetContainerID() As Integer Implements IContainer.GetContainerID
            Get
                Try
                    Dim ID As Integer = 0
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist) + Consts.ContainerIDOffset, ID, 4)
                    Return CInt(ID)
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public Shared Function ContainerHasParent(ByVal CIndex As Byte) As Boolean
            Try
                Dim HasP As Integer = 0
                Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (CIndex * Consts.ContainerDist) + Consts.ContainerHasParentOffset, HasP, 1)
                Return (HasP = 1)
            Catch Ex As Exception
                ShowError(Ex)
                End
            End Try
        End Function

        Public ReadOnly Property HasParent() As Boolean Implements IContainer.HasParent
            Get
                Try
                    Dim HasP As Integer = 0
                    Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist) + Consts.ContainerHasParentOffset, HasP, 1)
                    Return (HasP = 1)
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetContainerIndex() As Integer Implements IContainer.GetContainerIndex
            Get
                Return ContainerIndex
            End Get
        End Property

        Public ReadOnly Property Items(ByVal Index As Integer) As IContainer.ContainerItemDefinition Implements IContainer.Items
            Get
                Try
                    Dim Item As IContainer.ContainerItemDefinition
                    Dim ItemID As Integer
                    Dim ItemCount As Integer
                    If Index < Me.ContainerItemCount Then
                        Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist) + Consts.ContainerFirstItemOffset + (Index * Consts.ItemDist), ItemID, 4)
                        Kernel.Client.ReadMemory(Consts.ptrFirstContainer + (ContainerIndex * Consts.ContainerDist) + Consts.ContainerFirstItemOffset + (Index * Consts.ItemDist) + Consts.ItemCountOffset, ItemCount, 1)
                        Item.ID = CUShort(ItemID)
                        Item.Count = CInt(ItemCount)
                        Item.ContainerIndex = CInt(ContainerIndex)
                        Item.Slot = CInt(Index)
                    Else
                        Item.ID = 0
                        Item.Slot = 0
                        Item.ContainerIndex = 0
                        Item.Count = 0
                    End If
                    Return Item
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetInventorySlotId(ByVal Slot As ITibia.InventorySlots) As Integer Implements IContainer.GetInventorySlotId
            Get
                Try
                    Dim SlotId As Integer = 0
                    Kernel.Client.ReadMemory(Consts.ptrInventoryBegin + ((Slot - 1) * Consts.ItemDist), SlotId, 2)
                    Return SlotId
                Catch ex As Exception
                    ShowError(ex)
                End Try
            End Get
        End Property

        Public ReadOnly Property GetInventorySlotCount(ByVal Slot As ITibia.InventorySlots) As Integer Implements IContainer.GetInventorySlotCount
            Get
                Try
                    Dim SlotCount As Integer = 0
                    Kernel.Client.ReadMemory(Consts.ptrInventoryBegin + ((Slot) * Consts.ItemDist) + Consts.ItemCountOffset, SlotCount, 1)
                    Return SlotCount
                Catch ex As Exception
                    ShowError(ex)
                End Try
            End Get
        End Property

    End Class

    Public Class InternalContainer
        Public Index As Integer = 0
        Public ItemCount As Integer = 0
        Public Size As Integer = 0
        Public ID As Integer = 0
        Public Name As String = ""
        Public Parent As Boolean = False
        Public Item(19) As IContainer.ContainerItemDefinition

        Public Sub SetSize(ByVal NewSize As Integer)
            ReDim Item(NewSize)
            For I As Integer = 0 To NewSize
                Item(I).ID = 0
            Next
            Size = NewSize
        End Sub

        Public ReadOnly Property GetName() As String
            Get
                Try
                    Return Name
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetItemCount() As Integer
            Get
                Try
                    Return ItemCount
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetContainerSize() As Integer
            Get
                Try
                    Return Size
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetContainerID() As Integer
            Get
                Try
                    Return ID
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property HasParent() As Boolean
            Get
                Try
                    Return Parent
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public ReadOnly Property GetContainerIndex() As Integer
            Get
                Return Index
            End Get
        End Property

        Public ReadOnly Property Items(ByVal GetIndex As Integer) As IContainer.ContainerItemDefinition
            Get
                Try
                    Return Item(GetIndex)
                Catch Ex As Exception
                    ShowError(Ex)
                    End
                End Try
            End Get
        End Property

        Public Sub SetItem(ByVal InsertIndex As Integer, ByVal ToInsertItem As IContainer.ContainerItemDefinition)
            Item(InsertIndex) = ToInsertItem
        End Sub

        Public Sub AddItem(ByVal ID As UShort, ByVal Count As Integer)
            For ActualItem As Integer = ItemCount - 1 To 0 Step -1
                Item(ActualItem).Slot += 1
                SetItem(ActualItem + 1, Item(ActualItem))
            Next ActualItem
            Dim NewItem As IContainer.ContainerItemDefinition
            NewItem.ID = ID
            NewItem.Count = Count
            NewItem.ContainerIndex = Index
            NewItem.Slot = 0
            SetItem(0, NewItem)
            ItemCount += 1
        End Sub

        Public Sub RemoveItem(ByVal ItemSlot As Integer)
            Item(ItemSlot).ID = 0
            For ActualItem As Integer = ItemSlot + 1 To ItemCount - 1
                Item(ActualItem).Slot -= 1
                SetItem(ActualItem - 1, Item(ActualItem))
            Next ActualItem
            ItemCount -= 1
        End Sub

        Public Sub SetItemCount(ByVal ItemSlot As Integer, ByVal _Count As Integer)
            Item(ItemSlot).Count = _Count
        End Sub

    End Class

End Module
