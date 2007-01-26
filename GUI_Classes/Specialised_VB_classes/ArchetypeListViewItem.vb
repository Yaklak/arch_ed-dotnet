'
'
'	component:   "openEHR Archetype Project"
'	description: "$DESCRIPTION"
'	keywords:    "Archetype, Clinical, Editor"
'	author:      "Sam Heard"
'	support:     "Ocean Informatics <support@OceanInformatics.biz>"
'	copyright:   "Copyright (c) 2004,2005,2006 Ocean Informatics Pty Ltd"
'	license:     "See notice at bottom of class"
'
'	file:        "$URL$"
'	revision:    "$LastChangedRevision$"
'	last_change: "$LastChangedDate$"
'
'

Option Strict On

Public Class ArchetypeListViewItem : Inherits ListViewItem

    Private mArchetypeNode As ArchetypeNode

    Public ReadOnly Property Item() As ArchetypeNode
        Get
            Debug.Assert(Not mArchetypeNode Is Nothing)
            Return mArchetypeNode
        End Get
    End Property
    Public Shadows Property Text() As String
        Get
            Return mArchetypeNode.Text
        End Get
        Set(ByVal Value As String)
            MyBase.Text = Value
            mArchetypeNode.Text = Value
        End Set
    End Property

    

    Public Sub Translate()
        mArchetypeNode.Translate()
        MyBase.Text = mArchetypeNode.Text
    End Sub

    Public Function Copy() As ArchetypeListViewItem
        Return New ArchetypeListViewItem(Me.Item)
    End Function

    Public Sub Specialise()
        If TypeOf mArchetypeNode Is ArchetypeElement Then
            CType(mArchetypeNode, ArchetypeElement).Specialise()
            MyBase.Text = mArchetypeNode.Text
        End If
    End Sub


    Sub New(ByVal aText As String, ByVal a_file_manager As FileManagerLocal)
        MyBase.New(aText)
        mArchetypeNode = New ArchetypeElement(aText, a_file_manager)
    End Sub

    Sub New(ByVal el As RmElement, ByVal a_file_manager As FileManagerLocal)
        MyBase.New()
        'Must call translate to get the text
        Dim aTerm As RmTerm = a_file_manager.OntologyManager.GetTerm(el.NodeId)
        MyBase.Text = aTerm.Text

        mArchetypeNode = New ArchetypeElement(el, a_file_manager)

    End Sub

    Sub New(ByVal slot As RmSlot, ByVal a_file_manager As FileManagerLocal)
        MyBase.New()
        MyBase.Text = a_file_manager.OntologyManager.GetOpenEHRTerm(CInt(slot.SlotConstraint.RM_ClassType), slot.SlotConstraint.RM_ClassType.ToString)
        mArchetypeNode = New ArchetypeNodeAnonymous(slot)
    End Sub

    Sub New(ByVal an_archetype_node As ArchetypeNode)
        MyBase.New(an_archetype_node.Text)
        mArchetypeNode = an_archetype_node
    End Sub


End Class


'
'***** BEGIN LICENSE BLOCK *****
'Version: MPL 1.1/GPL 2.0/LGPL 2.1
'
'The contents of this file are subject to the Mozilla Public License Version 
'1.1 (the "License"); you may not use this file except in compliance with 
'the License. You may obtain a copy of the License at 
'http://www.mozilla.org/MPL/
'
'Software distributed under the License is distributed on an "AS IS" basis,
'WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
'for the specific language governing rights and limitations under the
'License.
'
'The Original Code is ArchetypeListViewItem.vb.
'
'The Initial Developer of the Original Code is
'Sam Heard, Ocean Informatics (www.oceaninformatics.biz).
'Portions created by the Initial Developer are Copyright (C) 2004
'the Initial Developer. All Rights Reserved.
'
'Contributor(s):
'	Heath Frankel
'
'Alternatively, the contents of this file may be used under the terms of
'either the GNU General Public License Version 2 or later (the "GPL"), or
'the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
'in which case the provisions of the GPL or the LGPL are applicable instead
'of those above. If you wish to allow use of your version of this file only
'under the terms of either the GPL or the LGPL, and not to allow others to
'use your version of this file under the terms of the MPL, indicate your
'decision by deleting the provisions above and replace them with the notice
'and other provisions required by the GPL or the LGPL. If you do not delete
'the provisions above, a recipient may use your version of this file under
'the terms of any one of the MPL, the GPL or the LGPL.
'
'***** END LICENSE BLOCK *****
'
