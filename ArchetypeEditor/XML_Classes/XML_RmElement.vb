'
'
'	component:   "openEHR Archetype Project"
'	description: "XML handling extensions to RmElement"
'	keywords:    "Archetype, Clinical, Editor"
'	author:      "Sam Heard"
'	support:     http://www.openehr.org/issues/browse/AEPR
'	copyright:   "Copyright (c) 2004,2005,2006 Ocean Informatics Pty Ltd"
'	license:     "See notice at bottom of class"
'
'	file:        "$URL: http://svn.openehr.org/knowledge_tools_dotnet/TRUNK/ArchetypeEditor/BusinessLogic/EHR_Classes/RmElement.vb $"
'	revision:    "$LastChangedRevision$"
'	last_change: "$LastChangedDate: 2006-05-17 18:54:30 +0930 (Wed, 17 May 2006) $"
'
'
Option Strict On
Option Explicit On

Namespace ArchetypeEditor.XML_Classes
    Public Class XML_RmElement
        Inherits RmElement

        Sub New(ByVal XML_Element As XMLParser.C_COMPLEX_OBJECT, ByVal a_filemanager As FileManagerLocal)
            MyBase.New(XML_Element)
            ProcessElement(XML_Element, a_filemanager)
        End Sub

#Region "Processing XML - incoming"

        Private Sub ProcessElement(ByVal ComplexObj As XMLParser.C_COMPLEX_OBJECT, ByVal a_filemanager As FileManagerLocal)

            Try
                If Not ComplexObj.attributes Is Nothing AndAlso ComplexObj.attributes.Length > 0 Then
                    For Each an_attribute As XMLParser.C_ATTRIBUTE In ComplexObj.attributes
                        Select Case an_attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                            Case "value"
                                If an_attribute.children.Length > 1 Then
                                    'Multiple constraints
                                    Dim m_c As New Constraint_Choice
                                    For Each cObject As XMLParser.C_OBJECT In an_attribute.children
                                        m_c.Constraints.Add(ProcessValue(cObject, a_filemanager))
                                    Next
                                    cConstraint = m_c
                                Else
                                    cConstraint = ProcessValue(CType(an_attribute.children(0), XMLParser.C_OBJECT), a_filemanager)
                                End If
                                'SRH 14th Nov 2007 - Added null flavor
                            Case "null_flavor", "null_flavour" 'SRH 30 Oct 2008 - new spelling added EDT-397
                                'Single attribute so no multiples
                                Dim c As Constraint_Text = ProcessText(CType(an_attribute.children(0), XMLParser.C_COMPLEX_OBJECT))
                                If Not c Is Nothing AndAlso c.AllowableValues.Codes.Count > 0 Then
                                    Me.ConstrainedNullFlavours = c.AllowableValues
                                End If
                        End Select
                    Next
                Else
                    'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                    'If ComplexObj.any_allowed Then
                    Dim complexObject As New C_COMPLEX_OBJECT_PROXY(ComplexObj)
                    If complexObject.Any_Allowed Then
                        'This is an unknown and is available for specialisation
                        cConstraint = New Constraint
                        Return
                    End If
                End If

                If cConstraint Is Nothing Then
                    'Failure of process so set to any
                    Debug.Assert(False, "Constraint not set")
                    cConstraint = New Constraint
                    Return
                End If

            Catch ex As Exception
                MessageBox.Show(AE_Constants.Instance.Incorrect_format & " " & ComplexObj.node_id & ": " & ex.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                ' set to any
                cConstraint = New Constraint
            End Try
        End Sub

        Private Function ProcessInterval(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT, ByVal a_filemanager As FileManagerLocal) As Constraint_Interval

            Dim an_attribute As XMLParser.C_ATTRIBUTE

            Select Case ObjNode.rm_type_name.ToLowerInvariant
                Case "interval_count"  'OBSOLETE
                    Dim cic As New Constraint_Interval_Count
                    Dim countLimits As Constraint_Count

                    an_attribute = CType(ObjNode.attributes(0), XMLParser.C_ATTRIBUTE)

                    countLimits = CType(ProcessValue(CType(an_attribute.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_Count)
                    If countLimits.HasMaximum Then
                        CType(cic.UpperLimit, Constraint_Count).HasMaximum = True
                        CType(cic.UpperLimit, Constraint_Count).MaximumValue = countLimits.MaximumValue
                    End If
                    If countLimits.HasMinimum Then
                        CType(cic.LowerLimit, Constraint_Count).HasMinimum = True
                        CType(cic.LowerLimit, Constraint_Count).MinimumValue = countLimits.MinimumValue
                    End If
                    Return cic
                Case "dv_interval<dv_count>", "dv_interval<count>", "interval<count>"
                    Dim cic As New Constraint_Interval_Count
                    Try
                        For Each attrib As XMLParser.C_ATTRIBUTE In ObjNode.attributes
                            Select Case attrib.rm_attribute_name
                                Case "upper"
                                    cic.UpperLimit = CType(ProcessValue(CType(attrib.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_Count)
                                Case "lower"
                                    cic.LowerLimit = CType(ProcessValue(CType(attrib.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_Count)
                            End Select
                        Next
                    Catch ex As Exception
                        MessageBox.Show(AE_Constants.Instance.Incorrect_format & " " & ObjNode.node_id & ": " & ex.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                    Return cic
                Case "interval_quantity"
                    Dim ciq As New Constraint_Interval_Quantity
                    Dim quantLimits As New Constraint_Quantity

                    an_attribute = CType(ObjNode.attributes(0), XMLParser.C_ATTRIBUTE)

                    quantLimits = CType(ProcessValue(CType(an_attribute.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_Quantity)
                    If quantLimits.HasUnits Then
                        ciq.AddUnit(CType(quantLimits.Units(0), Constraint_QuantityUnit))
                        CType(CType(ciq.UpperLimit, Constraint_Quantity).Units(0), Constraint_QuantityUnit).HasMinimum = False
                        CType(CType(ciq.LowerLimit, Constraint_Quantity).Units(0), Constraint_QuantityUnit).HasMaximum = False
                    End If
                    ciq.QuantityPropertyCode = quantLimits.OpenEhrCode
                    Return ciq
                Case "dv_interval<dv_quantity>", "dv_interval<quantity>", "interval<quantity>"
                    Dim ciq As New Constraint_Interval_Quantity
                    Try
                        For Each attrib As XMLParser.C_ATTRIBUTE In ObjNode.attributes
                            Select Case attrib.rm_attribute_name
                                Case "upper"
                                    ciq.UpperLimit = CType(ProcessValue(CType(attrib.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_Quantity)
                                Case "lower"
                                    ciq.LowerLimit = CType(ProcessValue(CType(attrib.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_Quantity)
                            End Select
                        Next
                    Catch ex As Exception
                        MessageBox.Show(AE_Constants.Instance.Incorrect_format & " " & ObjNode.node_id & ": " & ex.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                    Return ciq
                Case "dv_interval<dv_date_time>", "dv_interval<date_time>", "interval<date_time>", "dv_interval<dv_time>", "dv_interval<dv_date>"
                    Dim cidt As New Constraint_Interval_DateTime
                    Try
                        For Each attrib As XMLParser.C_ATTRIBUTE In ObjNode.attributes
                            Select Case attrib.rm_attribute_name
                                Case "upper"
                                    cidt.UpperLimit = CType(ProcessValue(CType(attrib.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_DateTime)
                                Case "lower"
                                    cidt.LowerLimit = CType(ProcessValue(CType(attrib.children(0), XMLParser.C_OBJECT), a_filemanager), Constraint_DateTime)
                            End Select
                        Next
                    Catch ex As Exception
                        MessageBox.Show(AE_Constants.Instance.Incorrect_format & " " & ObjNode.node_id & ": " & ex.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                    Return cidt

                Case Else
                    Debug.Assert(False)
                    Return Nothing
            End Select

        End Function

        Private Function ProcessMultiMedia(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT) As Constraint_MultiMedia
            Dim mm As New Constraint_MultiMedia
            Dim media_type As XMLParser.C_ATTRIBUTE

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
            'If ObjNode.any_allowed Then
            Dim complexObject As New C_COMPLEX_OBJECT_PROXY(ObjNode)
            If complexObject.Any_Allowed Then
                Return mm
            End If

            Try
                media_type = CType(ObjNode.attributes(0), XMLParser.C_ATTRIBUTE)
                Dim Obj As XMLParser.C_OBJECT

                Obj = CType(media_type.children(0), XMLParser.C_OBJECT)

                mm.AllowableValues = ArchetypeEditor.XML_Classes.XML_Tools.ProcessCodes(CType(Obj, XMLParser.C_CODE_PHRASE))

            Catch ex As Exception
                Debug.Assert(False)
                MessageBox.Show(AE_Constants.Instance.ErrorLoading & " Multimedia constraint:" & ObjNode.node_id & _
                    " - " & ex.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try

            Return mm

        End Function

        Private Function ProcessValue(ByVal ObjNode As XMLParser.C_OBJECT, ByVal a_filemanager As FileManagerLocal) As Constraint
            Select Case ObjNode.rm_type_name.ToLowerInvariant()
                Case "dv_quantity", "quantity"
                    Return ProcessQuantity(CType(ObjNode, XMLParser.C_DV_QUANTITY))
                Case "dv_coded_text", "dv_text", "coded_text", "text"
                    Return ProcessText(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                Case "dv_boolean", "boolean"
                    If TypeOf (ObjNode) Is XMLParser.C_COMPLEX_OBJECT Then
                        Return ProcessBoolean(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                    Else
                        'obsolete
                        Return ProcessBoolean(CType(ObjNode, XMLParser.C_PRIMITIVE_OBJECT))
                    End If
                Case "c_dv_ordinal", "ordinal", "c_ordinal", "dv_ordinal"
                    Return ProcessOrdinal(CType(ObjNode, XMLParser.C_DV_ORDINAL), a_filemanager)
                Case "dv_date_time", "dv_date", "dv_time", "date_time", "date", "time", "datetime"
                    If TypeOf (ObjNode) Is XMLParser.C_COMPLEX_OBJECT Then
                        Return ProcessDateTime(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                    Else
                        'obsolete
                        Return ProcessDateTime(CType(ObjNode, XMLParser.C_PRIMITIVE_OBJECT))
                    End If
                Case "dv_proportion", "proportion"
                    Return ProcessProportion(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                Case "dv_count", "count"
                    Return ProcessCount(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                Case "interval_count", "interval_quantity" 'OBSOLETE
                    Return ProcessInterval(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT), a_filemanager)
                Case "dv_interval<dv_count>", "dv_interval<count>", "interval<count>", _
                     "dv_interval<dv_quantity>", "dv_interval<quantity>", "interval<quantity>", _
                     "dv_interval<dv_date_time>", "dv_interval<date_time>", "interval<date_time>", _
                     "dv_interval<dv_date>", "dv_interval<dv_time>"
                    Return ProcessInterval(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT), a_filemanager)
                Case "dv_multimedia", "multi_media", "multimedia"
                    Return ProcessMultiMedia(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                Case "dv_uri", "uri", "dv_ehr_uri"
                    Return ProcessUri(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                Case "dv_identifier"
                    Return ProcessIdentifier(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                    'TODO Case "dv_currency"
                    '    Return ProcessCurrency(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                Case "dv_duration", "duration"
                    If TypeOf ObjNode Is XMLParser.C_COMPLEX_OBJECT Then
                        Return ProcessDuration(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                    Else
                        'obsolete
                        Return ProcessDuration(CType(ObjNode, XMLParser.C_PRIMITIVE_OBJECT))
                    End If
                Case "dv_parsable"
                    Return ProcessParsable(CType(ObjNode, XMLParser.C_COMPLEX_OBJECT))
                Case Else
                    Debug.Assert(False, String.Format("Attribute not handled: {0}", ObjNode.rm_type_name))
                    Return New Constraint
            End Select
        End Function

        Shared Function ProcessUri(ByVal dvUri As XMLParser.C_COMPLEX_OBJECT) As Constraint
            Dim cUri As New Constraint_URI
            Dim an_attribute As XMLParser.C_ATTRIBUTE

            If dvUri.rm_type_name.ToLowerInvariant = "dv_ehr_uri" Then
                cUri.EhrUriOnly = True
            End If

            If Not dvUri.attributes Is Nothing AndAlso dvUri.attributes.Length > 0 Then
                Try
                    an_attribute = CType(dvUri.attributes(0), XMLParser.C_ATTRIBUTE)
                    Debug.Assert(an_attribute.rm_attribute_name.ToLowerInvariant = "value")
                    If an_attribute.children.Length > 0 Then
                        Dim cadlOS As XMLParser.C_PRIMITIVE_OBJECT = _
                            CType(an_attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)
                        Dim cadlC As XMLParser.C_STRING = _
                            CType(cadlOS.item, XMLParser.C_STRING)

                        cUri.RegularExpression = cadlC.pattern
                    End If
                Catch e As Exception
                    MessageBox.Show(e.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
            Return cUri

        End Function

        Shared Function ProcessParsable(ByVal dvParse As XMLParser.C_COMPLEX_OBJECT) As Constraint
            Dim cParse As New Constraint_Parsable
            Dim an_attribute As XMLParser.C_ATTRIBUTE

            If Not dvParse.attributes Is Nothing AndAlso dvParse.attributes.Length > 0 Then


                Try

                    For i As Integer = 0 To dvParse.attributes.Length - 1

                        an_attribute = CType(dvParse.attributes(i), XMLParser.C_ATTRIBUTE)

                        Dim cadlOS As XMLParser.C_PRIMITIVE_OBJECT = _
                            CType(an_attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)
                        Dim cadlC As XMLParser.C_STRING = _
                            CType(cadlOS.item, XMLParser.C_STRING)

                        Select Case an_attribute.rm_attribute_name.ToLowerInvariant()
                            Case "value"
                                'obsolete
                                'cParse.RegularExpression = cadlC.pattern

                            Case "formalism"
                                Try

                                    cParse.AllowableFormalisms.AddRange(cadlC.list)

                                Catch ex As Exception
                                    Debug.Assert(False)
                                    MessageBox.Show(AE_Constants.Instance.ErrorLoading & " Parsable constraint:" & dvParse.node_id & _
                                        " - " & ex.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                                End Try
                        End Select

                    Next

                Catch e As Exception
                    MessageBox.Show(e.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try

            End If

            Return cParse
        End Function

        Function ProcessIdentifier(ByVal dvIdentifier As XMLParser.C_COMPLEX_OBJECT) As Constraint
            Dim cIdentifier As New Constraint_Identifier
            Dim an_attribute As XMLParser.C_ATTRIBUTE

            'SRH added check for null attributes EDT-444
            If Not dvIdentifier.attributes Is Nothing Then
                Try
                    For Each an_attribute In dvIdentifier.attributes
                        If an_attribute.children.Length > 0 Then
                            Dim cadlOS As XMLParser.C_PRIMITIVE_OBJECT = _
                                CType(an_attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)
                            Dim cadlC As XMLParser.C_STRING = _
                                CType(cadlOS.item, XMLParser.C_STRING)

                            Select Case an_attribute.rm_attribute_name.ToLowerInvariant()
                                Case "issuer"
                                    cIdentifier.IssuerRegex = cadlC.pattern
                                Case "type"
                                    cIdentifier.TypeRegex = cadlC.pattern
                                Case "id"
                                    cIdentifier.IDRegex = cadlC.pattern
                            End Select
                        End If
                    Next
                Catch e As Exception
                    MessageBox.Show(e.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If

            Return cIdentifier
        End Function

        Private Function ProcessDuration(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT) As Constraint_Duration
            Dim result As New Constraint_Duration

            If Not New C_COMPLEX_OBJECT_PROXY(ObjNode).Any_Allowed Then
                If ObjNode.attributes.Length > 0 Then
                    Dim attribute As XMLParser.C_ATTRIBUTE = ObjNode.attributes(0)

                    Select Case attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                        Case "value"
                            If Not attribute.children Is Nothing AndAlso attribute.children.Length > 0 Then
                                result = ProcessDuration(CType(attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT))
                            End If
                    End Select
                End If
            End If

            Return result
        End Function

        Private Function ProcessDuration(ByVal ObjNode As XMLParser.C_PRIMITIVE_OBJECT) As Constraint_Duration
            Dim result As New Constraint_Duration

            If Not New C_PRIMITIVE_OBJECT_PROXY(ObjNode).Any_Allowed Then
                Dim cadlC As XMLParser.C_DURATION = CType(ObjNode.item, XMLParser.C_DURATION)

                If Not String.IsNullOrEmpty(cadlC.pattern) Then
                    result.AllowableUnits = cadlC.pattern
                End If

                If Not cadlC.range Is Nothing Then
                    With cadlC.range
                        Debug.Assert(Not (.lower_included And .lower_unbounded), "lower included must not be true when unbounded")
                        Debug.Assert(Not (.upper_included And .upper_unbounded), "upper included must not be true when unbounded")
                        Debug.Assert(.lower_includedSpecified Or .lower_unbounded, "lower included specified must not equal lower unbounded")
                        Debug.Assert(.upper_includedSpecified Or .upper_unbounded, "upper included specified must not equal upper unbounded")
                    End With

                    If cadlC.range.lower_included Then
                        result.HasMinimum = True
                        result.MinMaxValueUnits = ArchetypeEditor.XML_Classes.XML_Tools.GetDurationUnits(cadlC.range.lower)

                        If cadlC.range.lower.StartsWith("P") Then
                            result.MinimumValue = Convert.ToInt64(Val(cadlC.range.lower.Substring(1))) ' leave the P of the front
                        Else
                            result.MinimumValue = Convert.ToInt64(Val(cadlC.range.lower))
                        End If
                    Else
                        result.HasMinimum = False
                    End If

                    If cadlC.range.upper_included Then
                        If result.MinMaxValueUnits = "" Then
                            result.MinMaxValueUnits = ArchetypeEditor.XML_Classes.XML_Tools.GetDurationUnits(cadlC.range.upper)
                        End If

                        result.HasMaximum = True

                        If cadlC.range.upper.StartsWith("P") Then
                            result.MaximumValue = Convert.ToInt64(Val(cadlC.range.upper.Substring(1))) ' leave the P of the front
                        Else
                            result.MaximumValue = Convert.ToInt64(Val(cadlC.range.upper))
                        End If
                    End If
                End If

                If Not cadlC.assumed_value Is Nothing Then
                    result.HasAssumedValue = True
                    result.AssumedValue = cadlC.assumed_value
                End If
            End If

            Return result
        End Function

        Private Function ProcessCount(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT) As Constraint_Count
            Dim ct As New Constraint_Count
            Dim an_attribute As XMLParser.C_ATTRIBUTE

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
            'If ObjNode.any_allowed Then
            Dim complexObject As New C_COMPLEX_OBJECT_PROXY(ObjNode)
            If complexObject.Any_Allowed Then
                Return ct
            End If

            For Each an_attribute In ObjNode.attributes
                Select Case an_attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                    Case "value", "magnitude"
                        Dim cadlOS As XMLParser.C_PRIMITIVE_OBJECT
                        Dim cadlC As XMLParser.C_INTEGER

                        cadlOS = CType(an_attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)
                        cadlC = CType(cadlOS.item, XMLParser.C_INTEGER)

                        'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                        'If cadlC.range.minimum <> "" Then
                        '    ct.HasMinimum = True
                        '    ct.MinimumValue = Long.Parse(cadlC.range.minimum)
                        '    ct.IncludeMinimum = cadlC.range.includes_minimum
                        'Else
                        '    ct.HasMinimum = False
                        'End If
                        'If cadlC.range.maximum <> "" Then
                        '    ct.HasMaximum = True
                        '    ct.MaximumValue = Long.Parse(cadlC.range.maximum)
                        '    ct.IncludeMaximum = cadlC.range.includes_maximum
                        'Else
                        '    ct.HasMaximum = False
                        'End If

                        ' Validate Interval PreConditions
                        With cadlC.range
                            Debug.Assert(.lowerSpecified = Not .lower_unbounded, "lower specified must not equal lower unbounded")
                            Debug.Assert(Not (.lower_included And .lower_unbounded), "lower included must not be true when unbounded")
                            Debug.Assert(.upperSpecified = Not .upper_unbounded, "upper specified must not equal upper unbounded")
                            Debug.Assert(Not (.upper_included And .upper_unbounded), "upper included must not be true when unbounded")
                            Debug.Assert(.lower_includedSpecified Or .lower_unbounded, "lower included specified must not equal lower unbounded")
                            Debug.Assert(.upper_includedSpecified Or .upper_unbounded, "upper included specified must not equal upper unbounded")
                        End With

                        If cadlC.range.lowerSpecified Then
                            ct.HasMinimum = True
                            ct.MinimumValue = CLng(cadlC.range.lower)
                            ct.IncludeMinimum = cadlC.range.lower_included
                        Else
                            ct.HasMinimum = False
                        End If
                        If cadlC.range.upperSpecified Then
                            ct.HasMaximum = True
                            ct.MaximumValue = CLng(cadlC.range.upper)
                            ct.IncludeMaximum = cadlC.range.upper_included
                        Else
                            ct.HasMaximum = False
                        End If
                        'If cadlC.assumed_value <> Nothing Then
                        If cadlC.assumed_valueSpecified Then
                            ct.HasAssumedValue = True
                            ct.AssumedValue = CInt(cadlC.assumed_value)
                        End If
                    Case Else
                        Debug.Assert(False, "Attribute not handled: " & an_attribute.rm_attribute_name)
                End Select
            Next
            Return ct
        End Function

        Private Function ProcessReal(ByVal ObjNode As XMLParser.C_PRIMITIVE_OBJECT) As Constraint_Real
            Dim ct As New Constraint_Real
            Dim cadlC As XMLParser.C_REAL

            cadlC = CType(ObjNode.item, XMLParser.C_REAL)

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
            'If cadlC.range.minimumSpecified Then
            '    ct.HasMinimum = True
            '    ct.MinimumValue = cadlC.range.minimum
            '    ct.IncludeMinimum = cadlC.range.includes_minimum
            'Else
            '    ct.HasMinimum = False
            'End If
            'If cadlC.range.maximumSpecified Then
            '    ct.HasMaximum = True
            '    ct.MaximumValue = cadlC.range.maximum
            '    ct.IncludeMaximum = cadlC.range.includes_maximum
            'Else
            '    ct.HasMaximum = False
            'End If

            ' Validate Interval PreConditions
            With cadlC.range
                Debug.Assert(.lowerSpecified = Not .lower_unbounded, "lower specified must not equal lower unbounded")
                Debug.Assert(Not (.lower_included And .lower_unbounded), "lower included must not be true when unbounded")
                Debug.Assert(.upperSpecified = Not .upper_unbounded, "upper specified must not equal upper unbounded")
                Debug.Assert(Not (.upper_included And .upper_unbounded), "upper included must not be true when unbounded")
                Debug.Assert(.lower_includedSpecified Or .lower_unbounded, "lower included specified must not equal lower unbounded")
                Debug.Assert(.upper_includedSpecified Or .upper_unbounded, "upper included specified must not equal upper unbounded")
            End With

            If cadlC.range.lowerSpecified Then
                ct.HasMinimum = True
                ct.MinimumRealValue = cadlC.range.lower
                ct.IncludeMinimum = cadlC.range.lower_included
            Else
                ct.HasMinimum = False
            End If
            If cadlC.range.upperSpecified Then
                ct.HasMaximum = True
                ct.MaximumRealValue = cadlC.range.upper
                ct.IncludeMaximum = cadlC.range.upper_included
            Else
                ct.HasMaximum = False
            End If
            If cadlC.assumed_valueSpecified Then
                'If cadlC.assumed_value <> Nothing Then
                ct.HasAssumedValue = True
                ct.AssumedValue = CInt(cadlC.assumed_value)
            End If

            Return ct

        End Function

        Private Function ProcessProportion(ByVal objNode As XMLParser.C_COMPLEX_OBJECT) As Constraint_Proportion
            Dim result As New Constraint_Proportion

            If Not New C_COMPLEX_OBJECT_PROXY(objNode).Any_Allowed Then
                For Each attribute As XMLParser.C_ATTRIBUTE In objNode.attributes
                    If Not attribute.children Is Nothing AndAlso attribute.children.Length > 0 Then
                        Select Case attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                            Case "numerator"
                                result.Numerator = ProcessReal(CType(attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT))
                            Case "denominator"
                                result.Denominator = ProcessReal(CType(attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT))
                            Case "is_integral"
                                Dim bool As Constraint_Boolean = ProcessBoolean(CType(attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT))
                                result.IsIntegral = bool.TrueAllowed
                                result.IsIntegralSet = True
                            Case "type"
                                Dim cadlOS As XMLParser.C_PRIMITIVE_OBJECT = CType(attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)
                                Dim cadlC As XMLParser.C_INTEGER = CType(cadlOS.item, XMLParser.C_INTEGER)
                                result.DisallowAllTypes()

                                If Not cadlC.list Is Nothing Then
                                    For ii As Integer = 0 To cadlC.list.Length - 1
                                        result.AllowType(CInt(cadlC.list(ii)))
                                    Next
                                ElseIf Not cadlC.range Is Nothing Then
                                    'is an interval as only one allowed
                                    result.AllowType(cadlC.range.upper)
                                Else
                                    result.AllowAllTypes()
                                End If
                            Case Else
                                Debug.Assert(False)
                        End Select
                    End If
                Next
            End If

            Return result
        End Function

        Private Function ProcessDateTime(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT) As Constraint_DateTime

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
            'If ObjNode.any_allowed Then
            Dim complexObject As New C_COMPLEX_OBJECT_PROXY(ObjNode)

            'SRH: 13 jan 2009 - EDT-497 - Allow all added to each type

            Dim result As New Constraint_DateTime()

            If complexObject.Any_Allowed Then
                Select Case complexObject.ConstraintObject.rm_type_name.ToUpperInvariant
                    Case "DV_DATE_TIME"
                        result.TypeofDateTimeConstraint = 11
                    Case "DV_DATE"
                        result.TypeofDateTimeConstraint = 14
                    Case "DV_TIME"
                        result.TypeofDateTimeConstraint = 18
                End Select
            Else
                Dim an_attribute As XMLParser.C_ATTRIBUTE

                For i As Integer = 0 To ObjNode.attributes.Length

                    an_attribute = ObjNode.attributes(i)
                    Select Case an_attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                        Case "value"
                            Dim constraint As XMLParser.C_PRIMITIVE_OBJECT
                            If an_attribute.children.Length > 0 Then
                                constraint = CType(an_attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)

                                'SRH: 13 jan 2009 - EDT-497 - Allow all added to each type
                                'Return ProcessDateTime(constraint)
                                result = ProcessDateTime(constraint)
                                Exit For
                            End If
                    End Select
                Next
            End If

            'SRH: 13 jan 2009 - EDT-497 - Allow all added to each type
            ''Shouldn't get to here
            'Debug.Assert(False, "Error processing DateTime")
            Return result

        End Function


        Private Function ProcessDateTime(ByVal ObjNode As XMLParser.C_PRIMITIVE_OBJECT) As Constraint_DateTime
            Dim dt As New Constraint_DateTime
            Dim s As String

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
            'If ObjNode.any_allowed Then
            Dim primitiveObject As New C_PRIMITIVE_OBJECT_PROXY(ObjNode)

            'SRH: 13 jan 2009 - EDT-497 - Allow all added to each type
            'If primitiveObject.Any_Allowed Then
            '    Return dt
            'End If

            Select Case ObjNode.rm_type_name.ToUpper(System.Globalization.CultureInfo.InvariantCulture)
                Case "DATE_TIME"
                    Dim cadlDT As XMLParser.C_DATE_TIME
                    cadlDT = CType(ObjNode.item, XMLParser.C_DATE_TIME)
                    If primitiveObject.Any_Allowed Then
                        s = "yyyy-??-??t??:??:??"
                    Else
                        s = cadlDT.pattern
                    End If

                Case "DATE"
                    Dim cadlD As XMLParser.C_DATE
                    cadlD = CType(ObjNode.item, XMLParser.C_DATE)
                    If primitiveObject.Any_Allowed Then
                        s = "yyyy-??-??"
                    Else
                        s = cadlD.pattern
                    End If
                Case "TIME"
                    Dim cadlT As XMLParser.C_TIME
                    cadlT = CType(ObjNode.item, XMLParser.C_TIME)
                    If primitiveObject.Any_Allowed Then
                        s = "t??:??:??"
                    Else
                        s = cadlT.pattern
                    End If

                Case Else
                    Debug.Assert(False)
                    Return Nothing
            End Select


            Select Case s.ToLowerInvariant
                Case "yyyy-??-?? ??:??:??", "yyyy-??-??t??:??:??"
                    ' Allow all
                    dt.TypeofDateTimeConstraint = 11
                Case "yyyy-mm-dd hh:mm:ss", "yyyy-mm-ddthh:mm:ss"
                    dt.TypeofDateTimeConstraint = 12
                Case "yyyy-mm-dd HH:??:??", "yyyy-mm-ddthh:??:??"
                    'Partial Date time
                    dt.TypeofDateTimeConstraint = 13
                Case "yyyy-??-??"
                    'Date only
                    dt.TypeofDateTimeConstraint = 14
                Case "yyyy-mm-dd"
                    'Full date
                    dt.TypeofDateTimeConstraint = 15
                Case "yyyy-??-xx"
                    'Partial date
                    dt.TypeofDateTimeConstraint = 16
                Case "yyyy-mm-??"
                    'Partial date with month
                    dt.TypeofDateTimeConstraint = 17
                Case "hh:??:??", "thh:??:??"
                    'TimeOnly
                    dt.TypeofDateTimeConstraint = 18
                Case "hh:mm:ss", "thh:mm:ss"
                    'Full time
                    dt.TypeofDateTimeConstraint = 19
                Case "hh:??:xx", "thh:??:xx"
                    'Partial time
                    dt.TypeofDateTimeConstraint = 20
                Case "hh:mm:??", "thh:mm:??"
                    'Partial time with minutes
                    dt.TypeofDateTimeConstraint = 21
                Case Else
                    Debug.Assert(False, "Pattern not handled: " & s)
                    dt.TypeofDateTimeConstraint = 11
            End Select

            Return dt

        End Function

        'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
        ''OBSOLETE
        'Private Function ProcessOrdinal(ByVal an_ordinal_value As XMLParser.C_ORDINAL, ByVal a_filemanager As FileManagerLocal) As Constraint_Ordinal
        '    Dim ehr_ordinal As XMLParser.ORDINAL

        '    Dim ord As New Constraint_Ordinal(a_filemanager)

        '    '' first value may have a "?" instead of a code as holder for empty ordinal
        '    For Each ehr_ordinal In an_ordinal_value.list

        '        Dim newOrdinal As OrdinalValue = ord.OrdinalValues.NewOrdinal

        '        newOrdinal.Ordinal = CInt(ehr_ordinal.value)

        '        If ehr_ordinal.symbol.terminology_id = "local" Then
        '            newOrdinal.InternalCode = ehr_ordinal.symbol.code_string
        '            ord.OrdinalValues.Add(newOrdinal)
        '        Else
        '            Debug.Assert(False)
        '        End If
        '    Next

        '    If an_ordinal_value.assumed_value <> Nothing Then
        '        ord.HasAssumedValue = True
        '        ord.AssumedValue = CInt(an_ordinal_value.assumed_value)
        '    End If

        '    Return ord

        'End Function

        Private Function ProcessOrdinal(ByVal an_ordinal_value As XMLParser.C_DV_ORDINAL, ByVal a_filemanager As FileManagerLocal) As Constraint_Ordinal
            'Dim ehr_ordinal As XMLParser.ORDINAL
            Dim ehr_ordinal As XMLParser.DV_ORDINAL

            Dim ord As New Constraint_Ordinal(a_filemanager)

            '' first value may have a "?" instead of a code as holder for empty ordinal

            If Not an_ordinal_value.list Is Nothing Then
                For Each ehr_ordinal In an_ordinal_value.list



                    'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                    'If ehr_ordinal.symbol.terminology_id = "local" Then
                    '    newOrdinal.InternalCode = ehr_ordinal.symbol.code_string
                    '    ord.OrdinalValues.Add(newOrdinal)
                    'Else
                    '    Debug.Assert(False)
                    'End If

                    If Not ehr_ordinal.symbol Is Nothing Then
                        If Not ehr_ordinal.symbol.defining_code Is Nothing Then
                            If Not ehr_ordinal.symbol.defining_code.terminology_id Is Nothing Then
                                If ehr_ordinal.symbol.defining_code.terminology_id.value = "local" Then
                                    'SRH: 31 May 2008 - check for redundancy in AT codes when adding ordinals (had a problem after manual cutting and pasting by some users
                                    If Array.IndexOf(ord.InternalCodes, ehr_ordinal.symbol.defining_code.code_string) = -1 Then
                                        Dim newOrdinal As OrdinalValue = ord.OrdinalValues.NewOrdinal

                                        newOrdinal.Ordinal = CInt(ehr_ordinal.value)
                                        newOrdinal.InternalCode = ehr_ordinal.symbol.defining_code.code_string
                                        ord.OrdinalValues.Add(newOrdinal)
                                    End If
                                End If
                            End If
                        End If
                    End If
                Next

                'If an_ordinal_value.assumed_value <> "" Then 'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                If Not an_ordinal_value.assumed_value Is Nothing Then
                    ord.HasAssumedValue = True
                    ord.AssumedValue = CInt(an_ordinal_value.assumed_value.value)
                End If
            End If
            Return ord

        End Function


        'OBSOLETE
        Private Function ProcessOrdinal(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT, ByVal a_filemanager As FileManagerLocal) As Constraint_Ordinal
            Dim c_value As XMLParser.C_DV_ORDINAL
            Dim an_attribute As XMLParser.C_ATTRIBUTE

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
            'If Not (ObjNode.any_allowed <> Nothing AndAlso ObjNode.any_allowed) Then
            Dim complexObject As New C_COMPLEX_OBJECT_PROXY(ObjNode)
            If Not complexObject.Any_Allowed Then
                For Each an_attribute In ObjNode.attributes

                    Select Case an_attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                        Case "value"
                            c_value = CType(an_attribute.children(0), XMLParser.C_DV_ORDINAL)
                            Return ProcessOrdinal(c_value, a_filemanager)
                    End Select
                Next

            End If

            Return New Constraint_Ordinal(True, a_filemanager)


        End Function

        Private Function ProcessBoolean(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT) As Constraint_Boolean
            Dim complexObject As New C_COMPLEX_OBJECT_PROXY(ObjNode)
            If complexObject.Any_Allowed Then
                Return New Constraint_Boolean
            Else

                Dim an_attribute As XMLParser.C_ATTRIBUTE

                For i As Integer = 0 To ObjNode.attributes.Length
                    an_attribute = ObjNode.attributes(i)
                    Select Case an_attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                        Case "value"
                            Dim constraint As XMLParser.C_PRIMITIVE_OBJECT
                            If an_attribute.children.Length > 0 Then
                                constraint = CType(an_attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)
                                Return ProcessBoolean(constraint)
                            End If
                    End Select
                Next
            End If

            'Shouldn't get to here
            Debug.Assert(False, "Error processing boolean")
            Return New Constraint_Boolean
        End Function

        Private Function ProcessBoolean(ByVal ObjSimple As XMLParser.C_PRIMITIVE_OBJECT) As Constraint_Boolean
            Dim result As New Constraint_Boolean
            Dim i As Integer = 0
            Dim bool As XMLParser.C_BOOLEAN = CType(ObjSimple.item, XMLParser.C_BOOLEAN)

            If bool.true_valid Then
                i = 1
            End If

            If bool.false_valid Then
                i += 2
            End If

            Select Case i
                Case 1
                    result.AllowTrueOnly()
                Case 2
                    result.AllowFalseOnly()
                Case 3, 0
                    result.AllowTrueOrFalse()
            End Select

            If bool.assumed_valueSpecified Then
                result.HasAssumedValue = True
                result.AssumedValue = bool.assumed_value
            End If

            Return result
        End Function

        Shared Function ProcessText(ByVal ObjNode As XMLParser.C_COMPLEX_OBJECT) As Constraint_Text
            Dim an_attribute As XMLParser.C_ATTRIBUTE
            Dim t As New Constraint_Text

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
            'If Not ObjNode.any_allowed = Nothing AndAlso ObjNode.any_allowed Then
            Dim complexObject As New C_COMPLEX_OBJECT_PROXY(ObjNode)
            If Not complexObject.Any_Allowed = Nothing AndAlso complexObject.Any_Allowed Then
                t.TypeOfTextConstraint = TextConstraintType.Text
                Return t
            End If

            For Each an_attribute In ObjNode.attributes
                Select Case an_attribute.rm_attribute_name.ToLower(System.Globalization.CultureInfo.InvariantCulture)
                    Case "defining_code", "code" 'code is redundant
                        'coded - could be a constraint or the actual codes so..
                        Dim Obj As XMLParser.C_OBJECT

                        Obj = CType(an_attribute.children(0), XMLParser.C_OBJECT)

                        Select Case Obj.GetType.ToString.ToUpper(System.Globalization.CultureInfo.InvariantCulture)
                            Case "XMLPARSER.CONSTRAINT_REF"
                                t.TypeOfTextConstraint = TextConstraintType.Terminology
                                t.ConstraintCode = CType(Obj, XMLParser.CONSTRAINT_REF).reference
                            Case "XMLPARSER.C_CODE_PHRASE"
                                t.AllowableValues = ArchetypeEditor.XML_Classes.XML_Tools.ProcessCodes(CType(Obj, XMLParser.C_CODE_PHRASE))

                                'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                                'If CType(Obj, XMLParser.C_CODE_PHRASE).assumed_value <> "" Then
                                If Not CType(Obj, XMLParser.C_CODE_PHRASE).assumed_value Is Nothing Then
                                    t.HasAssumedValue = True
                                    t.AssumedValue = CType(Obj, XMLParser.C_CODE_PHRASE).assumed_value.code_string()
                                    t.AssumedValue_TerminologyId = CType(Obj, XMLParser.C_CODE_PHRASE).assumed_value.terminology_id.value
                                End If

                                If t.AllowableValues.TerminologyID = "local" Or t.AllowableValues.TerminologyID = "openehr" Then
                                    t.TypeOfTextConstraint = TextConstraintType.Internal
                                End If
                        End Select


                    Case "value"
                        Dim constraint As XMLParser.C_PRIMITIVE_OBJECT
                        Dim cString As XMLParser.C_STRING

                        t.TypeOfTextConstraint = TextConstraintType.Text

                        If an_attribute.children.Length > 0 Then
                            constraint = CType(an_attribute.children(0), XMLParser.C_PRIMITIVE_OBJECT)
                            cString = CType(constraint.item, XMLParser.C_STRING)
                            t.AllowableValues.Codes.AddRange(cString.list)
                        End If

                    Case "mapping"
                        Debug.Assert(False)
                        ''fix me - need to deal with mappings
                End Select
            Next
            Return t

        End Function

        'Redundant
        'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
        'Private Function ProcessQuantity(ByVal ObjNode As XMLParser.C_QUANTITY) As Constraint_Quantity
        '    Dim q As New Constraint_Quantity
        '    Dim u As Constraint_QuantityUnit

        '    If ObjNode.any_allowed <> Nothing AndAlso ObjNode.any_allowed Then
        '        Return q
        '    End If

        '    If Not ObjNode.property Is Nothing Then

        '        q.OpenEhrCode = CInt(ObjNode.property.code_string)

        '        If Not ObjNode.list Is Nothing Then

        '            For Each cqi As XMLParser.C_QUANTITY_ITEM In ObjNode.list
        '                'Do not set attribute to true here as there
        '                'is no special handling of time units until Unit is set
        '                u = New Constraint_QuantityUnit(q.IsTime)

        '                u.Unit = cqi.units

        '                If Not cqi.magnitude Is Nothing Then
        '                    u.HasMaximum = (cqi.magnitude.maximum <> Nothing)
        '                    If u.HasMaximum Then
        '                        u.MaximumValue = cqi.magnitude.maximum
        '                        u.IncludeMaximum = cqi.magnitude.includes_maximum
        '                    End If
        '                    u.HasMinimum = (cqi.magnitude.minimumSpecified)
        '                    If u.HasMinimum Then
        '                        u.MinimumValue = cqi.magnitude.minimum
        '                        u.IncludeMinimum = cqi.magnitude.includes_minimum
        '                    End If
        '                End If

        '                If cqi.precision <> "-1" Then
        '                    u.Precision = CInt(cqi.precision)
        '                End If

        '                If cqi.assumed_value <> Nothing Then
        '                    u.HasAssumedValue = True
        '                    u.AssumedValue = cqi.assumed_value
        '                End If

        '                ' need to add with key for retrieval
        '                q.Units.Add(u, u.Unit)
        '            Next
        '        End If
        '    End If

        '    Return q

        'End Function

        Private Function ProcessQuantity(ByVal ObjNode As XMLParser.C_DV_QUANTITY) As Constraint_Quantity
            Dim q As New Constraint_Quantity
            Dim u As Constraint_QuantityUnit

            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1 
            'If ObjNode.any_allowed <> Nothing AndAlso ObjNode.any_allowed Then
            Dim DvQuantity As New C_DV_QUANTITY_PROXY(ObjNode)
            If DvQuantity.Any_Allowed <> Nothing AndAlso DvQuantity.Any_Allowed Then
                Return q
            End If

            If Not ObjNode.property Is Nothing Then

                q.OpenEhrCode = CInt(ObjNode.property.code_string)

                If Not ObjNode.list Is Nothing Then

                    For Each cqi As XMLParser.C_QUANTITY_ITEM In ObjNode.list
                        'Do not set attribute to true here as there
                        'is no special handling of time units until Unit is set
                        u = New Constraint_QuantityUnit(q.IsTime)

                        u.Unit = cqi.units

                        'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                        'If Not cqi.magnitude Is Nothing Then
                        '    u.HasMaximum = (cqi.magnitude.maximum <> Nothing)
                        '    If u.HasMaximum Then
                        '        u.MaximumValue = cqi.magnitude.maximum
                        '        u.IncludeMaximum = cqi.magnitude.includes_maximum
                        '    End If
                        '    u.HasMinimum = (cqi.magnitude.minimumSpecified)
                        '    If u.HasMinimum Then
                        '        u.MinimumValue = cqi.magnitude.minimum
                        '        u.IncludeMinimum = cqi.magnitude.includes_minimum
                        '    End If
                        'End If

                        If Not cqi.magnitude Is Nothing Then

                            ' Validate Interval PreConditions
                            With cqi.magnitude
                                Debug.Assert(.lowerSpecified = Not .lower_unbounded, "lower specified must not equal lower unbounded")
                                Debug.Assert(Not (.lower_included And .lower_unbounded), "lower included must not be true when unbounded")
                                Debug.Assert(.upperSpecified = Not .upper_unbounded, "upper specified must not equal upper unbounded")
                                Debug.Assert(Not (.upper_included And .upper_unbounded), "upper included must not be true when unbounded")
                                Debug.Assert(.lower_includedSpecified Or .lower_unbounded, "lower included specified must not equal lower unbounded")
                                Debug.Assert(.upper_includedSpecified Or .upper_unbounded, "upper included specified must not equal upper unbounded")
                            End With

                            u.HasMaximum = (cqi.magnitude.upperSpecified)
                            If u.HasMaximum Then
                                u.MaximumRealValue = cqi.magnitude.upper
                                u.IncludeMaximum = cqi.magnitude.upper_included
                            End If
                            u.HasMinimum = (cqi.magnitude.lowerSpecified)
                            If u.HasMinimum Then
                                u.MinimumRealValue = cqi.magnitude.lower
                                u.IncludeMinimum = cqi.magnitude.lower_included
                            End If
                        End If

                        If Not cqi.precision Is Nothing Then
                            'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                            'If cqi.precision <> "-1" Then
                            If cqi.precision.lower <> -1 Then
                                Debug.Assert(cqi.precision.lower = cqi.precision.upper)
                                'u.Precision = CInt(cqi.precision)                            
                                u.Precision = cqi.precision.lower
                            End If
                        End If

                        'JAR: 30APR2007, AE-42 Support XML Schema 1.0.1
                        'If cqi.assumed_value <> Nothing Then
                        '    u.HasAssumedValue = True
                        '    u.AssumedValue = cqi.assumed_value
                        'End If                        
                        If Not ObjNode.assumed_value Is Nothing Then
                            If StrComp(ObjNode.assumed_value.units, u.Unit, CompareMethod.Text) = 0 Then 'assumed value is for this unit
                                u.AssumedValue = ObjNode.assumed_value.magnitude
                                u.HasAssumedValue = True
                            End If
                        End If

                        ' need to add with key for retrieval
                        q.Units.Add(u, u.Unit)
                    Next
                End If
            End If

            Return q

        End Function

#End Region

    End Class
End Namespace
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
'The Original Code is XML_RmElement.vb.
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
