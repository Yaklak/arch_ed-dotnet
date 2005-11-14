'
'
'	component:   "openEHR Archetype Project"
'	description: "$DESCRIPTION"
'	keywords:    "Archetype, Clinical, Editor"
'	author:      "Sam Heard"
'	support:     "Ocean Informatics <support@OceanInformatics.biz>"
'	copyright:   "Copyright (c) 2004 Ocean Informatics Pty Ltd"
'	license:     "See notice at bottom of class"
'
'	file:        "$Source$"
'	revision:    "$Revision$"
'	last_change: "$Date$"
'
'

Option Explicit On 

Class RmHistory
    Inherits RmStructureCompound

    Private colEvt As New EventCollection
    Private boolIsPeriodic As Boolean
    Private iPeriod As Integer
    Private sPeriodUnits As String
    Private rmData As RmStructureCompound

    Public Property isPeriodic() As Boolean
        Get
            Return boolIsPeriodic
        End Get
        Set(ByVal Value As Boolean)
            boolIsPeriodic = Value
        End Set
    End Property
    Public Property Period() As Integer
        Get
            Return iPeriod
        End Get
        Set(ByVal Value As Integer)
            iPeriod = Value
        End Set
    End Property
    Public Property PeriodUnits() As String
        Get
            Return sPeriodUnits
        End Get
        Set(ByVal Value As String)
            sPeriodUnits = Value
        End Set
    End Property
    Public Property Data() As RmStructureCompound
        Get
            Return rmData
        End Get
        Set(ByVal Value As RmStructureCompound)
            rmData = Value
        End Set
    End Property
    Public Shadows ReadOnly Property Children() As EventCollection
        Get
            Return colEvt
        End Get
    End Property

    Public Overrides Function copy() As RmStructure
        Dim rme As RmHistory
        rme.colEvt = colEvt.Copy
        rme.cOccurrences = Me.cOccurrences
        rme.sNodeId = Me.sNodeId
        Return rme
    End Function

    Sub New(ByVal EIF_history As Openehr.am.C_COMPLEX_OBJECT)
        MyBase.new(EIF_history)
        ProcessHistory(EIF_history)
    End Sub

    Sub New(ByVal NodeId As String)
        MyBase.new(NodeId, StructureType.History)
        MyBase.cOccurrences.MinCount = 1
    End Sub

    Public Overrides Function GetChildByNodeId(ByVal aNodeId As String) As RmStructure

        Dim child As RmStructure '= MyBase.GetChildByNodeId(aNodeId)
        'If child Is Nothing Then
        child = Data.GetChildByNodeId(aNodeId)
        'end if
        If child Is Nothing Then
            child = Children.GetChildByNodeId(aNodeId)
        End If

        Return child
    End Function

    Private Sub ProcessHistory(ByVal ObjNode As Openehr.am.C_COMPLEX_OBJECT)
        Dim an_attribute As Openehr.am.C_Attribute
        Dim period As Openehr.am.C_Primitive_Object
        Dim i As Integer

        cOccurrences = ADL_Tools.Instance.SetOccurrences(ObjNode.occurrences)

        For i = 1 To ObjNode.Attributes.Count
            an_attribute = ObjNode.Attributes.i_th(i)
            Select Case an_attribute.Rm_Attribute_Name.to_cil
                Case "name", "Name", "NAME", "runtime_label", "Runtime_label", "RUNTIME_LABEL"
                    mRuntimeConstraint = RmElement.ProcessText(CType(an_attribute.children.first, openehr.am.C_COMPLEX_OBJECT))

                Case "period", "Period", "PERIOD"
                    Dim d As Duration

                    period = an_attribute.children.first
                    d.ISO_duration = period.item.as_string.to_cil
                    iPeriod = d.GUI_duration
                    sPeriodUnits = d.GUI_Units

                Case "events", "Events", "EVENTS"
                    Dim an_Event As openehr.am.C_COMPLEX_OBJECT
                    Dim ii As Integer

                    ' empty the remembered structure
                    ADL_Tools.Instance.LastProcessedStructure = Nothing

                    colEvt.Cardinality.SetFromOpenEHRCardinality(an_attribute.cardinality)

                    For ii = 1 To an_attribute.children.count
                        Dim Struct_rel_node As openehr.am.C_ATTRIBUTE
                        an_Event = an_attribute.children.i_th(ii)
                        ' process the event and expose the data structure if it is present
                        ' as ADL_Data property
                        ' this means there is only one structure per history as in the GUI -
                        ' can be extended in future

                        colEvt.Add(New RmEvent(an_Event))
                    Next

                    ' the data definition is on one event at present
                    ' this is passed to the ADL_tools during event processing
                    ' and placed on the history at this point

                    If Not ADL_Tools.Instance.LastProcessedStructure Is Nothing Then
                        rmData = ADL_Tools.Instance.LastProcessedStructure
                    End If

            End Select
        Next
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
'The Original Code is RmHistory.vb.
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