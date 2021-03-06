'
'
'	component:   "openEHR Archetype Project"
'	description: "$DESCRIPTION"
'	keywords:    "Archetype, Clinical, Editor"
'	author:      "Sam Heard"
'	support:     http://www.openehr.org/issues/browse/AEPR
'	copyright:   "Copyright (c) 2004,2005,2006 Ocean Informatics Pty Ltd"
'	license:     "See notice at bottom of class"
'
'

Public MustInherit Class ElementViewControl : Inherits Control

    Public Event ValueChanged As EventHandler

    Protected mToolTips As New ToolTip
    Protected mFileManager As FileManagerLocal

    Public Sub New(ByVal anElement As ArchetypeElement, ByVal a_filemanager As FileManagerLocal)
        MyBase.New()
        TabStop = False
        mFileManager = a_filemanager

        Dim location As New Point(0, 0)
        Dim lbl As New Label
        lbl.Width = 100
        lbl.Height = 35
        location.Y += 5
        lbl.Location = location
        lbl.Text = anElement.Text & ":"

        ' cardinality and description as a tooltip
        Dim s As String

        If anElement.Description <> "*" Then
            s = anElement.Description & " (" & anElement.Occurrences.ToString & ")"
        Else
            s = anElement.Occurrences.ToString
        End If

        mToolTips.SetToolTip(lbl, s)

        If anElement.Occurrences.MinCount > 0 Then
            lbl.Font = New System.Drawing.Font(lbl.Font, FontStyle.Bold)
        End If

        If anElement.Constraint IsNot Nothing Then
            Select Case anElement.Constraint.Kind
                Case ConstraintKind.Text
                    lbl.TextAlign = ContentAlignment.TopLeft
                Case ConstraintKind.DateTime
                    lbl.TextAlign = ContentAlignment.MiddleLeft
            End Select
        End If

        Controls.Add(lbl)

        ' remember new position
        location.X = lbl.Right + 25

        If anElement.Constraint IsNot Nothing Then
            InitialiseComponent(anElement.Constraint, location)
        End If

        SetSize()
    End Sub

    Public Sub New(ByVal aConstraint As Constraint, ByVal a_filemanager As FileManagerLocal)
        MyBase.New()
        mFileManager = a_filemanager
        InitialiseComponent(aConstraint, New Point(0, 0))
        SetSize()
    End Sub

    Protected MustOverride Sub InitialiseComponent(ByVal aConstraint As Constraint, ByVal aLocation As Point)

    Protected Sub SetSize()
        Dim s As New Size

        For Each ctrl As Control In Controls
            If ctrl.Visible Then
                s.Width = Math.Max(s.Width, ctrl.Right)
                s.Height = Math.Max(s.Height, ctrl.Bottom)
            End If
        Next

        Size = s
    End Sub

#If PaintBoundary Then
    Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.DrawRectangle(New Pen(Color.Green), 0, 0, Size.Width - 1, Size.Height - 1)
    End Sub
#End If

    Public Overridable Property Value() As Object
        Get
            Debug.Assert(False)
            Return Nothing
        End Get
        Set(ByVal Value As Object)
            Debug.Assert(False)
        End Set
    End Property

    Protected Sub OnValueChanged()
        RaiseEvent ValueChanged(Me, New EventArgs)
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
'The Original Code is ElementViewControl.vb.
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
