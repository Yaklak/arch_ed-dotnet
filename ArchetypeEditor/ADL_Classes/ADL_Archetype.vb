'
'
'	component:   "openEHR Archetype Project"
'	description: "Builds all ADL Archetypes"
'	keywords:    "Archetype, Clinical, Editor"
'	author:      "Sam Heard"
'	support:     "Ocean Informatics <support@OceanInformatics.biz>"
'	copyright:   "Copyright (c) 2004,2005,2006 Ocean Informatics Pty Ltd"
'	license:     "See notice at bottom of class"
'
'	file:        "$Source: source/vb.net/archetype_editor/ADL_Classes/SCCS/s.ADL_Archetype.vb $"
'	revision:    "$LastChangedRevision$"
'	last_change: "$LastChangedDate$"
'
'

Option Explicit On 

Namespace ArchetypeEditor.ADL_Classes

    Public Class ADL_Archetype
        Inherits Archetype

        'Builds all archetypes at present

        Protected adlArchetype As openehr.openehr.am.archetype.ARCHETYPE
        Protected adlEngine As openehr.adl_parser.syntax.adl.ADL_ENGINE
        Protected mCADL_Factory As openehr.openehr.am.archetype.constraint_model.CONSTRAINT_MODEL_FACTORY

        Protected Structure ReferenceToResolve
            Dim Element As RmElement
            Dim Attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
        End Structure

        Protected ReferencesToResolve As ArrayList = New ArrayList


        Public Overrides Property ConceptCode() As String
            Get
                Return adlArchetype.concept_code.to_cil
            End Get
            Set(ByVal Value As String)
                adlArchetype.set_concept(openehr.base.kernel.Create.STRING.make_from_cil(Value))
            End Set
        End Property
        Public Overrides ReadOnly Property ArchetypeAvailable() As Boolean
            Get
                Return adlEngine.archetype_available
            End Get
        End Property
        Public Overrides Property Archetype_ID() As ArchetypeID
            Get
                Try
                    Return mArchetypeID
                Catch
                    Debug.Assert(False)
                    Return Nothing
                End Try
            End Get
            Set(ByVal Value As ArchetypeID)
                SetArchetypeId(Value)
            End Set
        End Property
        Public Overrides Property LifeCycle() As String
            Get
                Return sLifeCycle
            End Get
            Set(ByVal Value As String)
                sLifeCycle = Value
            End Set
        End Property
        Public Overrides Property ParentArchetype() As String
            Get
                Return adlArchetype.parent_archetype_id.as_string.to_cil
            End Get
            Set(ByVal Value As String)
                adlArchetype.set_parent_archetype_id(openehr.base.kernel.Create.STRING.make_from_cil(Value))
            End Set
        End Property
        Public Overrides ReadOnly Property SourceCode() As String
            Get
                If Not adlEngine.source Is Nothing Then
                    Return adlEngine.source.to_cil
                Else
                    Return Nothing
                End If
            End Get
        End Property
        Public Overrides ReadOnly Property SerialisedArchetype(ByVal a_format As String) As String
            Get
                Me.MakeParseTree()
                Try
                    adlEngine.serialise(openehr.base.kernel.Create.STRING.make_from_cil(a_format))
                    Return adlEngine.serialised_archetype.to_cil
                Catch e As Exception
                    MessageBox.Show(e.Message, AE_Constants.Instance.MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return AE_Constants.Instance.Error_saving
                End Try
            End Get
        End Property
        Public Overrides ReadOnly Property Paths(ByVal LanguageCode As String, ByVal parserIsSynchronised As Boolean, Optional ByVal Logical As Boolean = False) As String()
            Get
                Dim list As openehr.base.structures.list.ARRAYED_LIST_ANY
                Dim i As Integer
                ' must call the prepareToSave to ensure it is accurate
                If (Not Filemanager.Master.FileLoading) AndAlso (Not parserIsSynchronised) Then
                    MakeParseTree()
                End If
                ' showing the task with logical paths takes a lot of space
                If Logical Then
                    list = adlArchetype.logical_paths(openehr.base.kernel.Create.STRING.make_from_cil(LanguageCode))
                Else
                    list = adlArchetype.physical_paths()
                End If

                Dim s(list.upper - 1) As String

                For i = list.lower() To list.upper()
                    s(i - 1) = CType(list.i_th(i), openehr.base.kernel.STRING).to_cil()
                Next
                Return s
            End Get
        End Property

        Public Overrides Sub Specialise(ByVal ConceptShortName As String, ByRef The_Ontology As OntologyManager)
            Dim a_term As ADL_Term

            adlEngine.specialise_archetype(openehr.base.kernel.Create.STRING.make_from_cil(ConceptShortName))
            ' Update the GUI tables with the new term
            a_term = New ADL_Term(adlEngine.ontology.term_definition(openehr.base.kernel.Create.STRING.make_from_cil(The_Ontology.LanguageCode), adlArchetype.concept_code))
            The_Ontology.UpdateTerm(a_term)
            Me.mArchetypeID.Concept &= "-" & ConceptShortName


        End Sub

        Public Sub RemoveUnusedCodes()
            adlArchetype.ontology_remove_unused_codes()
        End Sub

        Protected Sub SetArchetypeId(ByVal an_archetype_id As ArchetypeID)
            Dim id As openehr.openehr.rm.support.identification.ARCHETYPE_ID

            id = openehr.openehr.rm.support.identification.Create.ARCHETYPE_ID.make_from_string(openehr.base.kernel.Create.STRING.make_from_cil(an_archetype_id.ToString))
            Try
                If Not adlEngine.archetype_available Then
                    adlEngine.create_new_archetype(id.rm_originator, id.rm_name, id.rm_entity, openehr.base.kernel.Create.STRING.make_from_cil(sPrimaryLanguageCode))
                    adlArchetype = adlEngine.archetype
                    adlArchetype.definition.set_object_id(adlArchetype.concept_code)
                    setDefinition()
                Else
                    ' does this involve a change in the entity (affects the GUI a great deal!)
                    If Not id.rm_entity.is_equal(adlArchetype.archetype_id.rm_entity) Then
                        Debug.Assert(False, "Not handled")
                        ' will need to reset the GUI to the new entity
                        setDefinition()
                    End If
                End If
                adlArchetype.set_archetype_id(id)
                ' set the internal variable last in case errors
                mArchetypeID = an_archetype_id
            Catch e As Exception
                Debug.Assert(False, "Error setting archetype id")
                Beep()
            End Try
        End Sub

        Protected Sub ArchetypeID_Changed(ByVal sender As Object, ByVal e As EventArgs) Handles mArchetypeID.ArchetypeID_Changed
            SetArchetypeId(CType(sender, ArchetypeID))
        End Sub

        Protected Function MakeAssertion(ByVal id As String, ByVal expression As String) As openehr.openehr.am.archetype.assertion.ASSERTION
            Dim id_expression_leaf, id_pattern_expression_leaf As openehr.openehr.am.archetype.assertion.EXPR_LEAF
            Dim match_operator As openehr.openehr.am.archetype.assertion.EXPR_BINARY_OPERATOR

            Debug.Assert((Not id Is Nothing) And (id <> ""))

            id_expression_leaf = mCADL_Factory.create_expr_leaf_object_ref(openehr.base.kernel.Create.STRING.make_from_cil(id))
            If expression = "*" Then
                id_pattern_expression_leaf = mCADL_Factory.create_expr_leaf_constraint(mCADL_Factory.create_c_string_make_from_regexp(openehr.base.kernel.Create.STRING.make_from_cil(expression)))
            Else
                id_pattern_expression_leaf = mCADL_Factory.create_expr_leaf_constraint(mCADL_Factory.create_c_string_make_from_regexp(openehr.base.kernel.Create.STRING.make_from_cil(expression)))
            End If
            match_operator = mCADL_Factory.create_expr_binary_operator_node( _
                openehr.openehr.am.archetype.assertion.Create.OPERATOR_KIND.make_from_string( _
                    openehr.base.kernel.Create.STRING.make_from_cil("matches")), _
                id_expression_leaf, id_pattern_expression_leaf)

            Return mCADL_Factory.create_assertion(match_operator, Nothing)

        End Function

        Protected Function MakeCardinality(ByVal c As RmCardinality, Optional ByVal IsOrdered As Boolean = True) As openehr.openehr.am.archetype.constraint_model.CARDINALITY
            Dim cardObj As openehr.openehr.am.archetype.constraint_model.CARDINALITY

            If c.IsUnbounded Then
                cardObj = mCADL_Factory.create_cardinality_make_upper_unbounded(c.MinCount)
            Else
                cardObj = mCADL_Factory.create_cardinality_make_bounded(c.MinCount, c.MaxCount)
            End If
            If Not c.Ordered Then
                cardObj.set_unordered()
            End If
            Return cardObj

        End Function

        Protected Function MakeOccurrences(ByVal c As RmCardinality) As openehr.common_libs.basic.OE_INTERVAL_INT32

            If c.IsUnbounded Then
                Return mCADL_Factory.create_c_integer_make_upper_unbounded(c.MinCount, c.IncludeLower).interval
            Else
                Return mCADL_Factory.create_c_integer_make_bounded(c.MinCount, c.MaxCount, c.IncludeLower, c.IncludeUpper).interval
            End If
        End Function

        Protected Overloads Sub BuildCodedText(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal ConstraintID As String)
            Dim coded_text As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim code_rel_node As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim ca_Term As openehr.openehr.am.archetype.constraint_model.CONSTRAINT_REF


            coded_text = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil("CODED_TEXT"))
            code_rel_node = mCADL_Factory.create_c_attribute_single(coded_text, openehr.base.kernel.Create.STRING.make_from_cil("code"))
            ca_Term = openehr.openehr.am.archetype.constraint_model.Create.CONSTRAINT_REF.make(openehr.base.kernel.Create.STRING.make_from_cil(ConstraintID))
            code_rel_node.put_child(ca_Term)
        End Sub

        Protected Overloads Sub BuildCodedText(ByRef ObjNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT, ByVal RunTimeName As String)
            Dim coded_text, codePhrase As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim code_rel_node, name_rel_node As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim ca_Term As openehr.openehr.am.archetype.constraint_model.CONSTRAINT_REF

            name_rel_node = mCADL_Factory.create_c_attribute_single(ObjNode, openehr.base.kernel.Create.STRING.make_from_cil("name"))
            coded_text = mCADL_Factory.create_c_complex_object_anonymous(name_rel_node, openehr.base.kernel.Create.STRING.make_from_cil("CODED_TEXT"))
            code_rel_node = mCADL_Factory.create_c_attribute_single(coded_text, openehr.base.kernel.Create.STRING.make_from_cil("code"))
            ca_Term = openehr.openehr.am.archetype.constraint_model.Create.CONSTRAINT_REF.make(openehr.base.kernel.Create.STRING.make_from_cil(RunTimeName))
            code_rel_node.put_child(ca_Term)
        End Sub

        Protected Overloads Sub BuildCodedText(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal a_CodePhrase As CodePhrase, Optional ByVal an_assumed_value As String = "")
            Dim coded_text As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim code_rel_node As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim ca_Term As openehr.openehr.am.openehr_profile.data_types.text.C_CODED_TERM

            coded_text = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil("CODED_TEXT"))

            code_rel_node = mCADL_Factory.create_c_attribute_single(coded_text, openehr.base.kernel.Create.STRING.make_from_cil("code"))
            If a_CodePhrase.Codes.Count > 0 Then
                ca_Term = mCADL_Factory.create_c_coded_term_from_pattern(code_rel_node, openehr.base.kernel.Create.STRING.make_from_cil(a_CodePhrase.Phrase))
                If an_assumed_value <> "" Then
                    ca_Term.set_assumed_value(openehr.base.kernel.Create.STRING.make_from_cil(an_assumed_value))
                End If
            Else
                ca_Term = openehr.openehr.am.openehr_profile.data_types.text.Create.C_CODED_TERM.make_from_terminology_id(openehr.base.kernel.Create.STRING.make_from_cil(a_CodePhrase.TerminologyID))
                code_rel_node.put_child(ca_Term)
            End If
        End Sub

        Protected Sub BuildPlainText(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal TermList As Collections.Specialized.StringCollection)
            Dim plain_text As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim value_rel_node As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim cString As openehr.openehr.am.archetype.constraint_model.primitive.OE_C_STRING
            Dim cadlSimple As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT

            plain_text = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil("TEXT"))

            If TermList.Count > 0 Then
                Dim i As Integer
                value_rel_node = mCADL_Factory.create_c_attribute_single(plain_text, openehr.base.kernel.Create.STRING.make_from_cil("value"))
                cString = mCADL_Factory.create_c_string_make_from_string(openehr.base.kernel.Create.STRING.make_from_cil(TermList.Item(0)))
                For i = 1 To TermList.Count - 1
                    cString.add_string(openehr.base.kernel.Create.STRING.make_from_cil(TermList.Item(i)))
                Next
                cadlSimple = mCADL_Factory.create_c_primitive_object(value_rel_node, cString)
            Else
                plain_text.set_any_allowed()
            End If

        End Sub

        Private Sub DuplicateHistory(ByVal rm As RmStructureCompound, ByRef RelNode As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE)

            Dim cadlHistory, cadlEvent As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim an_event As RmEvent
            Dim rm_1 As RmStructureCompound
            Dim a_history As RmHistory

            For Each rm_1 In cDefinition.Data
                If rm_1.Type = StructureType.History Then
                    a_history = CType(rm_1, RmHistory)
                    cadlHistory = mCADL_Factory.create_c_complex_object_identified(RelNode, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(StructureType.History)), openehr.base.kernel.Create.STRING.make_from_cil(a_history.NodeId))
                    cadlHistory.set_occurrences(MakeOccurrences(a_history.Occurrences))
                    If Not a_history.HasNameConstraint Then
                        an_attribute = mCADL_Factory.create_c_attribute_single(cadlHistory, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                        BuildText(an_attribute, a_history.NameConstraint)
                    End If
                    If a_history.isPeriodic Then
                        Dim period As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT
                        Dim d As Duration = New ArchetypeEditor.ADL_Classes.Duration

                        an_attribute = mCADL_Factory.create_c_attribute_single(cadlHistory, openehr.base.kernel.Create.STRING.make_from_cil("period"))
                        d.ISO_Units = OceanArchetypeEditor.ISO_TimeUnits.GetISOForLanguage(a_history.PeriodUnits)
                        d.GUI_duration = a_history.Period

                        period = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_duration_make_bounded(openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), True, True))
                    End If

                    ' now build the events
                    If a_history.Children.Count > 0 Then
                        an_attribute = mCADL_Factory.create_c_attribute_multiple(cadlHistory, openehr.base.kernel.Create.STRING.make_from_cil("events"), MakeCardinality(a_history.Children.Cardinality))
                        an_event = a_history.Children.Item(0)
                        cadlEvent = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(StructureType.Event)), openehr.base.kernel.Create.STRING.make_from_cil(an_event.NodeId))
                        cadlEvent.set_occurrences(MakeOccurrences(an_event.Occurrences))
                        If an_event.isPointInTime Then
                            If an_event.hasFixedOffset Then
                                Dim offset As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT
                                Dim d As Duration = New ArchetypeEditor.ADL_Classes.Duration

                                an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("offset"))
                                d.ISO_Units = OceanArchetypeEditor.ISO_TimeUnits.GetISOForLanguage(an_event.OffsetUnits)
                                d.GUI_duration = an_event.Offset
                                offset = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_duration_make_bounded(openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), True, True))
                            End If
                        Else
                            Dim width As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT

                            If an_event.AggregateMathFunction <> "" Then
                                an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("math_function"))
                                Dim a_code_phrase As CodePhrase = New CodePhrase
                                a_code_phrase.FirstCode = an_event.AggregateMathFunction
                                a_code_phrase.TerminologyID = "openehr"
                                BuildCodedText(an_attribute, a_code_phrase)
                            End If

                            If an_event.hasFixedDuration Then
                                Dim d As Duration = New ArchetypeEditor.ADL_Classes.Duration

                                an_attribute = mCADL_Factory.create_c_attribute_single(cadlHistory, openehr.base.kernel.Create.STRING.make_from_cil("width"))
                                d.ISO_Units = OceanArchetypeEditor.ISO_TimeUnits.GetISOForLanguage(an_event.WidthUnits)
                                d.GUI_duration = an_event.Width
                                width = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_duration_make_bounded(openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), True, True))
                            End If
                        End If

                        ' runtime name
                        If an_event.HasNameConstraint Then
                            an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                            BuildText(an_attribute, an_event.NameConstraint)
                        End If

                        ' data
                        an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("data"))
                        Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                        objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(rm.Type)), openehr.base.kernel.Create.STRING.make_from_cil(rm.NodeId))
                        BuildStructure(rm, objNode)

                        Exit Sub

                    End If ' at least one child
                End If
            Next

        End Sub

        Private Sub BuildHistory(ByVal a_history As RmHistory, ByRef RelNode As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE)
            Dim cadlHistory, cadlEvent As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim events_rel_node, an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim an_event As RmEvent
            Dim data_processed As Boolean
            Dim data_path As openehr.common_libs.structures.object_graph.path.OG_PATH

            cadlHistory = mCADL_Factory.create_c_complex_object_identified(RelNode, _
                openehr.base.kernel.Create.STRING.make_from_cil(StructureType.History.ToString.ToUpper(System.Globalization.CultureInfo.InvariantCulture)), _
                openehr.base.kernel.Create.STRING.make_from_cil(a_history.NodeId))
            cadlHistory.set_occurrences(MakeOccurrences(a_history.Occurrences))

            If a_history.HasNameConstraint Then
                an_attribute = mCADL_Factory.create_c_attribute_single(cadlHistory, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                BuildText(an_attribute, a_history.NameConstraint)
            End If

            If a_history.isPeriodic Then
                Dim period As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT
                Dim d As Duration = New ArchetypeEditor.ADL_Classes.Duration

                an_attribute = mCADL_Factory.create_c_attribute_single(cadlHistory, openehr.base.kernel.Create.STRING.make_from_cil("period"))
                d.ISO_Units = a_history.PeriodUnits
                d.GUI_duration = a_history.Period

                period = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_duration_make_bounded(openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), True, True))
            End If

            ' now build the events

            events_rel_node = mCADL_Factory.create_c_attribute_multiple(cadlHistory, openehr.base.kernel.Create.STRING.make_from_cil("events"), MakeCardinality(a_history.Children.Cardinality))

            For Each an_event In a_history.Children
                cadlEvent = mCADL_Factory.create_c_complex_object_identified(events_rel_node, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(an_event.Type)), openehr.base.kernel.Create.STRING.make_from_cil(an_event.NodeId))
                cadlEvent.set_occurrences(MakeOccurrences(an_event.Occurrences))

                Select Case an_event.Type
                    Case StructureType.Event
                        ' do nothing...
                    Case StructureType.PointEvent
                        If an_event.hasFixedOffset Then
                            Dim offset As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT
                            Dim d As Duration = New ArchetypeEditor.ADL_Classes.Duration

                            an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("offset"))
                            d.ISO_Units = an_event.OffsetUnits
                            d.GUI_duration = an_event.Offset
                            offset = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_duration_make_bounded(openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), True, True))
                        End If
                    Case StructureType.IntervalEvent
                        Dim width As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT

                        If an_event.AggregateMathFunction <> "" Then
                            an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("math_function"))
                            Dim a_code_phrase As CodePhrase = New CodePhrase
                            a_code_phrase.FirstCode = an_event.AggregateMathFunction
                            a_code_phrase.TerminologyID = "openehr"
                            BuildCodedText(an_attribute, a_code_phrase)
                        End If

                        If an_event.hasFixedDuration Then
                            Dim d As Duration = New ArchetypeEditor.ADL_Classes.Duration

                            an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("width"))
                            d.ISO_Units = an_event.WidthUnits
                            d.GUI_duration = an_event.Width
                            width = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_duration_make_bounded(openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), openehr.base.kernel.Create.STRING.make_from_cil(d.ISO_duration), True, True))
                        End If
                End Select

                ' runtime name
                If an_event.HasNameConstraint Then
                    an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                    BuildText(an_attribute, an_event.NameConstraint)
                End If

                ' data
                
                If Not data_processed Then
                    If Not a_history.Data Is Nothing Then

                        an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("data"))

                        Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT


                        objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(a_history.Data.Type)), openehr.base.kernel.Create.STRING.make_from_cil(a_history.Data.NodeId))
                        BuildStructure(a_history.Data, objNode)

                        ''data_path = cadlEvent.path
                        data_path = GetPathOfNode(a_history.Data.NodeId)
                    Else
                        cadlEvent.set_any_allowed()
                    End If
                    data_processed = True
                Else
                    Dim NodeRef As openehr.openehr.am.archetype.constraint_model.ARCHETYPE_INTERNAL_REF
                    If Not cadlEvent.has_attribute(openehr.base.kernel.Create.STRING.make_from_cil("data")) Then
                        an_attribute = mCADL_Factory.create_c_attribute_single(cadlEvent, openehr.base.kernel.Create.STRING.make_from_cil("data"))
                    End If
                    NodeRef = mCADL_Factory.create_archetype_internal_ref(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(a_history.Data.Type)), data_path.as_string)
                End If

            Next

        End Sub

        Protected Sub BuildCluster(ByVal Cluster As RmCluster, ByRef RelNode As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE)
            Dim cluster_cadlObj As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim rm As RmStructure

            cluster_cadlObj = mCADL_Factory.create_c_complex_object_identified(RelNode, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(StructureType.Cluster)), openehr.base.kernel.Create.STRING.make_from_cil(Cluster.NodeId))
            cluster_cadlObj.set_occurrences(MakeOccurrences(Cluster.Occurrences))

            If Cluster.HasNameConstraint Then
                an_attribute = mCADL_Factory.create_c_attribute_single(cluster_cadlObj, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                BuildText(an_attribute, Cluster.NameConstraint)
            End If

            If Cluster.Children.Count > 0 Then
                an_attribute = mCADL_Factory.create_c_attribute_multiple(cluster_cadlObj, openehr.base.kernel.Create.STRING.make_from_cil("items"), MakeCardinality(Cluster.Children.Cardinality, Cluster.Children.Cardinality.Ordered))
                For Each rm In Cluster.Children.items
                    If rm.Type = StructureType.Cluster Then
                        BuildCluster(rm, an_attribute)
                    Else
                        BuildElementOrReference(rm, an_attribute)
                    End If
                Next
            Else
                cluster_cadlObj.set_any_allowed()
            End If
        End Sub

        Private Sub BuildRatio(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal cr As Constraint_Ratio)
            Dim RatioObject As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim fraction_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

            RatioObject = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil("QUANTITY_RATIO"))
            fraction_attribute = mCADL_Factory.create_c_attribute_single(RatioObject, openehr.base.kernel.Create.STRING.make_from_cil("numerator"))
            BuildCount(fraction_attribute, cr.Numerator)
            fraction_attribute = mCADL_Factory.create_c_attribute_single(RatioObject, openehr.base.kernel.Create.STRING.make_from_cil("denominator"))
            BuildCount(fraction_attribute, cr.Denominator)

        End Sub

        Protected Sub BuildCount(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal ct As Constraint_Count)
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim cadlCount As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim magnitude As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT

            cadlCount = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_DataTypeName(ct.Type)))

            If ct.HasMaximum Or ct.HasMinimum Then
                ' set the magnitude constraint
                an_attribute = mCADL_Factory.create_c_attribute_single(cadlCount, openehr.base.kernel.Create.STRING.make_from_cil("magnitude"))

                If ct.HasMaximum And ct.HasMinimum Then
                    magnitude = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_integer_make_bounded(ct.MinimumValue, ct.MaximumValue, ct.IncludeMinimum, ct.IncludeMaximum))
                ElseIf ct.HasMaximum Then
                    magnitude = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_integer_make_lower_unbounded(ct.MaximumValue, ct.IncludeMaximum))
                ElseIf ct.HasMinimum Then
                    magnitude = mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_integer_make_upper_unbounded(ct.MinimumValue, ct.IncludeMinimum))
                End If

                If ct.HasAssumedValue Then
                    Dim int_ref As openehr.base.kernel.INTEGER_REF

                    int_ref = openehr.base.kernel.Create.INTEGER_REF.default_create
                    int_ref.set_item(CType(ct.AssumedValue, Integer))

                    CType(magnitude.item, openehr.openehr.am.archetype.constraint_model.primitive.Impl.C_INTEGER).set_assumed_value(int_ref)
                End If
            Else
                cadlCount.set_any_allowed()
            End If
        End Sub

        Private Sub BuildDateTime(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal dt As Constraint_DateTime)
            Dim s As String
            Dim dtType As String
            Dim cadlDateTime As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT

            Select Case dt.TypeofDateTimeConstraint
                Case 11                 ' Allow all
                    s = "yyyy-??-?? ??:??:??"
                    dtType = "dt"
                Case 12                 ' Full date time
                    s = "yyyy-mm-dd HH:MM:SS"
                    dtType = "dt"
                Case 13                 'Partial Date time
                    s = "yyyy-mm-dd HH:??:XX"
                    dtType = "dt"
                Case 14                 'Date only
                    s = "yyyy-??-??"
                    dtType = "d"
                Case 15                'Full date
                    s = "yyyy-mm-dd"
                    dtType = "d"
                Case 16                'Partial date
                    s = "yyyy-??-XX"
                    dtType = "d"
                Case 17                'Partial date with month
                    s = "yyyy-mm-XX"
                    dtType = "d"
                Case 18                'TimeOnly
                    s = "HH:??:??"
                    dtType = "t"
                Case 19                 'Full time
                    s = "HH:MM:SS"
                    dtType = "t"
                Case 20                'Partial time
                    s = "HH:??:XX"
                    dtType = "t"
                Case 21                'Partial time with minutes
                    s = "HH:MM:XX"
                    dtType = "t"
            End Select

            Select Case dtType
                Case "dt"
                    cadlDateTime = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_date_time_make_pattern(openehr.base.kernel.Create.STRING.make_from_cil(s)))
                Case "d"
                    cadlDateTime = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_date_make_pattern(openehr.base.kernel.Create.STRING.make_from_cil(s)))
                Case "t"
                    cadlDateTime = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_time_make_pattern(openehr.base.kernel.Create.STRING.make_from_cil(s)))
            End Select

        End Sub

        Protected Sub BuildSlot(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal a_slot As RmSlot)
            BuildSlot(value_attribute, a_slot.SlotConstraint, a_slot.Occurrences)
        End Sub

        Protected Sub BuildSlot(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal sl As Constraint_Slot, ByVal an_occurrence As RmCardinality)
            Dim slot As openehr.openehr.am.archetype.constraint_model.ARCHETYPE_SLOT

            slot = mCADL_Factory.create_archetype_slot_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(sl.RM_ClassType)))

            slot.set_occurrences(MakeOccurrences(an_occurrence))

            If sl.hasSlots Then
                If sl.IncludeAll Then
                    slot.add_include(MakeAssertion("domain_concept", ".*"))
                Else
                    For Each s As String In sl.Include
                        slot.add_include(MakeAssertion("domain_concept", s))
                    Next
                End If
                If sl.ExcludeAll Then
                    slot.add_exclude(MakeAssertion("domain_concept", ".*"))
                Else
                    For Each s As String In sl.Exclude
                        slot.add_exclude(MakeAssertion("domain_concept", s))
                    Next
                End If
                Debug.Assert(slot.has_excludes Or slot.has_includes)
            Else
                slot.add_include(MakeAssertion("domain_concept", ".*"))
            End If

        End Sub

        Protected Sub BuildQuantity(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal q As Constraint_Quantity)
            Dim cadlQuantity As openehr.openehr.am.openehr_profile.data_types.quantity.C_QUANTITY

            cadlQuantity = mCADL_Factory.create_c_quantity(value_attribute)
            ' set the property constraint - it should be present

            If Not q.IsNull Then

                Dim cp As openehr.openehr.rm.data_types.text.CODE_PHRASE

                Debug.Assert(q.IsCoded)

                cp = openehr.openehr.rm.data_types.text.Create.CODE_PHRASE.make_from_string( _
                 openehr.base.kernel.Create.STRING.make_from_cil(q.PhysicalPropertyAsString))

                cadlQuantity.set_property(cp)

                If q.has_units Then
                    Dim unit_constraint As Constraint_QuantityUnit

                    For Each unit_constraint In q.Units
                        Dim a_real As openehr.common_libs.basic.OE_INTERVAL_SINGLE

                        If unit_constraint.HasMaximum Or unit_constraint.HasMinimum Then
                            If unit_constraint.HasMaximum And unit_constraint.HasMinimum Then
                                a_real = mCADL_Factory.create_real_interval_make_bounded(unit_constraint.MinimumValue, unit_constraint.MaximumValue, unit_constraint.IncludeMinimum, unit_constraint.IncludeMaximum)
                            ElseIf unit_constraint.HasMaximum Then
                                a_real = mCADL_Factory.create_real_interval_make_lower_unbounded(unit_constraint.MaximumValue, unit_constraint.IncludeMaximum)
                            ElseIf unit_constraint.HasMinimum Then
                                a_real = mCADL_Factory.create_real_interval_make_upper_unbounded(unit_constraint.MinimumValue, unit_constraint.IncludeMinimum)
                            End If
                        Else
                            'a_real = mCADL_Factory.create_real_interval_make_unbounded()
                            a_real = Nothing
                        End If

                        If unit_constraint.HasAssumedValue Then
                            cadlQuantity.set_assumed_value_from_units_magnitude(openehr.base.kernel.Create.STRING.make_from_cil(unit_constraint.Unit), unit_constraint.AssumedValue)
                        End If
                        cadlQuantity.add_unit_constraint(openehr.base.kernel.Create.STRING.make_from_cil(unit_constraint.Unit), a_real)
                    Next
                End If

            Else
                cadlQuantity.set_any_allowed()
            End If

        End Sub

        Private Sub BuildBoolean(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal b As Constraint_Boolean)
            Dim c_value As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT

            If b.TrueFalseAllowed Then
                c_value = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_boolean_make_true_false())
            ElseIf b.TrueAllowed Then
                c_value = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_boolean_make_true())
            ElseIf b.FalseAllowed Then
                c_value = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_boolean_make_false())
            End If

            'ToDo: See if this works again!

            'If b.hasAssumedValue Then
            '    If b.AssumedValue = True Then
            '        c_value = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_boolean_make_true())
            '    Else
            '        c_value = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_boolean_make_false())
            '    End If
            'Else
            '    c_value = mCADL_Factory.create_c_primitive_object(value_attribute, mCADL_Factory.create_c_boolean_make_true_false())
            'End If
        End Sub

        Protected Sub BuildOrdinal(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal o As Constraint_Ordinal)
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim an_object As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim c_value As openehr.openehr.am.openehr_profile.data_types.quantity.C_ORDINAL
            Dim o_v As OrdinalValue

            an_object = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_DataTypeName(o.Type)))

            If o.OrdinalValues.Count > 0 Then
                an_attribute = mCADL_Factory.create_c_attribute_single(an_object, openehr.base.kernel.Create.STRING.make_from_cil("value"))
                c_value = mCADL_Factory.create_c_ordinal(an_attribute)

                For Each o_v In o.OrdinalValues
                    Dim cadlO As openehr.openehr.am.openehr_profile.data_types.quantity.ORDINAL
                    cadlO = mCADL_Factory.create_ordinal(o_v.Ordinal, openehr.base.kernel.Create.STRING.make_from_cil("local::" & o_v.InternalCode))
                    c_value.add_item(cadlO)
                Next
                If o.HasAssumedValue Then
                    an_attribute = mCADL_Factory.create_c_attribute_single(an_object, openehr.base.kernel.Create.STRING.make_from_cil("assumed_value"))
                    mCADL_Factory.create_c_primitive_object(an_attribute, mCADL_Factory.create_c_integer_make_bounded(o.AssumedValue, o.AssumedValue, True, True))
                End If
            Else
                an_object.set_any_allowed()
            End If

        End Sub

        Protected Sub BuildText(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal t As Constraint_Text)

            Select Case t.TypeOfTextConstraint
                Case TextConstrainType.Terminology
                    If t.ConstraintCode <> "" Then
                        BuildCodedText(value_attribute, t.ConstraintCode)
                    End If
                Case TextConstrainType.Internal
                    BuildCodedText(value_attribute, t.AllowableValues, t.AssumedValue)
                Case TextConstrainType.Text
                    BuildPlainText(value_attribute, t.AllowableValues.Codes)
            End Select
        End Sub

        Protected Function GetPathOfNode(ByVal NodeId As String) As openehr.common_libs.structures.object_graph.path.OG_PATH
            Dim arraylist As openehr.Base.structures.list.LIST_ANY
            Dim path As openehr.common_libs.structures.object_graph.path.OG_PATH
            Dim s As String
            Dim i As Integer

            arraylist = Me.adlArchetype.definition.all_paths

            For i = 1 To arraylist.count
                s = arraylist.i_th(i).out.to_cil
                If s.EndsWith(NodeId & "]") Then
                    path = openehr.common_libs.structures.object_graph.path.Create.OG_PATH.make_from_string(openehr.base.kernel.Create.STRING.make_from_cil(s))
                    Return path
                End If
            Next
            Debug.Assert(False, "Should be a path for every node")
            Return Nothing

        End Function

        Private Sub BuildInterval(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal c As Constraint_Interval)

            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

            objNode = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_DataTypeName(c.Type)))

            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            an_attribute = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("absolute_limits"))

            BuildElementConstraint(an_attribute, c.AbsoluteLimits)

        End Sub

        Private Sub BuildMultiMedia(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal c As Constraint_MultiMedia)
            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim code_rel_node As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim ca_Term As openehr.openehr.am.openehr_profile.data_types.text.C_CODED_TERM

            objNode = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_DataTypeName(c.Type)))

            code_rel_node = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("media_type"))
            If c.AllowableValues.Codes.Count > 0 Then
                ca_Term = mCADL_Factory.create_c_coded_term_from_pattern(code_rel_node, openehr.base.kernel.Create.STRING.make_from_cil(c.AllowableValues.Phrase))
            Else
                ca_Term = openehr.openehr.am.openehr_profile.data_types.text.Create.C_CODED_TERM.make_from_terminology_id(openehr.base.kernel.Create.STRING.make_from_cil(c.AllowableValues.TerminologyID))
                code_rel_node.put_child(ca_Term)
            End If

        End Sub

        Private Sub BuildDuration(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal c As Constraint_Duration)
            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim d As openehr.openehr.am.archetype.constraint_model.primitive.C_DURATION

            d = openehr.openehr.am.archetype.constraint_model.primitive.Create.C_DURATION.make_from_pattern(openehr.base.kernel.Create.STRING.make_from_cil(c.AllowableUnits))

            Dim po As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT
            po = openehr.openehr.am.archetype.constraint_model.Create.C_PRIMITIVE_OBJECT.make(d)
            value_attribute.put_child(po)

            'objNode = mCADL_Factory.create_c_primitive_object(value_attribute, d)

        End Sub

        Private Sub BuildURI(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal c As Constraint_URI)
            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

            objNode = mCADL_Factory.create_c_complex_object_anonymous(value_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_DataTypeName(c.Type)))
            objNode.set_any_allowed()
        End Sub

        Protected Sub BuildElementConstraint(ByVal value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE, ByVal c As Constraint)

            ' cannot have a value with no constraint on datatype
            Debug.Assert(c.Type <> ConstraintType.Any)

            Select Case c.Type
                Case ConstraintType.Quantity
                    BuildQuantity(value_attribute, c)

                Case ConstraintType.Boolean
                    BuildBoolean(value_attribute, c)

                Case ConstraintType.Text
                    BuildText(value_attribute, c)

                Case ConstraintType.Ordinal
                    BuildOrdinal(value_attribute, c)

                Case ConstraintType.Any
                    value_attribute.parent.set_any_allowed()

                Case ConstraintType.Ratio
                    BuildRatio(value_attribute, c)

                Case ConstraintType.Count
                    BuildCount(value_attribute, c)

                Case ConstraintType.Ratio
                    BuildRatio(value_attribute, c)

                Case ConstraintType.DateTime
                    BuildDateTime(value_attribute, c)

                Case ConstraintType.Slot
                    BuildSlot(value_attribute, c, New RmCardinality)

                Case ConstraintType.Multiple
                    For Each a_constraint As Constraint In CType(c, Constraint_Choice).Constraints
                        BuildElementConstraint(value_attribute, a_constraint)
                    Next

                Case ConstraintType.Interval_Count, ConstraintType.Interval_Quantity
                    BuildInterval(value_attribute, c)

                Case ConstraintType.MultiMedia
                    BuildMultiMedia(value_attribute, c)

                Case ConstraintType.URI
                    BuildURI(value_attribute, c)

                Case ConstraintType.Duration
                    BuildDuration(value_attribute, c)
            End Select

        End Sub
        Protected Sub BuildElementOrReference(ByVal Element As RmElement, ByRef RelNode As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE)
            Dim value_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

            If Element.Type = StructureType.Reference Then
                Dim ref_cadlRefNode As openehr.openehr.am.archetype.constraint_model.ARCHETYPE_INTERNAL_REF
                Dim path As openehr.common_libs.structures.object_graph.path.OG_PATH

                Dim ref As ReferenceToResolve
                ref.Element = Element
                ref.Attribute = RelNode

                ReferencesToResolve.Add(ref)

            Else
                Dim element_cadlObj As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
                Dim name_rel_Node, Definition_rel_node As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

                element_cadlObj = mCADL_Factory.create_c_complex_object_identified(RelNode, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(StructureType.Element)), openehr.base.kernel.Create.STRING.make_from_cil(Element.NodeId))
                element_cadlObj.set_occurrences(MakeOccurrences(Element.Occurrences))
                If Element.HasNameConstraint Then
                    Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

                    an_attribute = mCADL_Factory.create_c_attribute_single(element_cadlObj, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                    BuildText(an_attribute, Element.NameConstraint)
                End If
                If Element.Constraint.Type = ConstraintType.Any Then
                    If element_cadlObj.attributes.count = 0 Then
                        element_cadlObj.set_any_allowed()
                    End If
                Else
                    value_attribute = mCADL_Factory.create_c_attribute_single(element_cadlObj, openehr.base.kernel.Create.STRING.make_from_cil("value"))
                    BuildElementConstraint(value_attribute, Element.Constraint)
                End If

            End If
        End Sub

        Private Sub BuildStructure(ByVal rmStruct As RmStructureCompound, ByRef objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim rm As RmStructure

            ' preconditions
            Debug.Assert(rmStruct.NodeId <> "") ' anonymous

            ' now make sure there are some contents to the structure
            ' and if not set it to anyallowed
            If rmStruct.Children.Count > 0 Then
                Select Case rmStruct.Type '.TypeName
                    Case StructureType.Single ' "SINGLE"

                        an_attribute = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("item"))
                        BuildElementOrReference(rmStruct.Children.items(0), an_attribute)

                    Case StructureType.List ' "LIST"
                        an_attribute = mCADL_Factory.create_c_attribute_multiple(objNode, openehr.base.kernel.Create.STRING.make_from_cil("items"), MakeCardinality(CType(rmStruct, RmStructureCompound).Children.Cardinality, CType(rmStruct, RmStructureCompound).Children.Cardinality.Ordered))
                        For Each rm In rmStruct.Children.items
                            BuildElementOrReference(rm, an_attribute)
                        Next
                    Case StructureType.Tree ' "TREE"
                        an_attribute = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("items"))
                        an_attribute.set_cardinality(MakeCardinality(CType(rmStruct, RmStructureCompound).Children.Cardinality, CType(rmStruct, RmStructureCompound).Children.Cardinality.Ordered))
                        For Each rm In rmStruct.Children.items
                            'If rm.TypeName = "Cluster" Then
                            If rm.Type = StructureType.Cluster Then
                                BuildCluster(rm, an_attribute)
                            Else
                                BuildElementOrReference(rm, an_attribute)
                            End If
                        Next
                    Case StructureType.Table ' "TABLE"
                        Dim rm_c As RmStructureCompound
                        Dim table As RmTable
                        Dim b As openehr.openehr.am.archetype.constraint_model.primitive.C_BOOLEAN
                        Dim rh As openehr.openehr.am.archetype.constraint_model.primitive.C_INTEGER

                        table = CType(rmStruct, RmTable)
                        ' set is rotated
                        an_attribute = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("rotated"))
                        If table.isRotated Then
                            b = mCADL_Factory.create_c_boolean_make_true()
                        Else
                            b = mCADL_Factory.create_c_boolean_make_false()
                        End If
                        mCADL_Factory.create_c_primitive_object(an_attribute, b)

                        ' set number of row if not one
                        If table.NumberKeyColumns > 0 Then
                            an_attribute = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("number_key_columns"))
                            rh = mCADL_Factory.create_c_integer_make_bounded(table.NumberKeyColumns, table.NumberKeyColumns, True, True)
                            mCADL_Factory.create_c_primitive_object(an_attribute, rh)
                        End If


                        an_attribute = mCADL_Factory.create_c_attribute_multiple(objNode, openehr.base.kernel.Create.STRING.make_from_cil("rows"), MakeCardinality(New RmCardinality(rmStruct.Occurrences), True))

                        BuildCluster(rmStruct.Children.items(0), an_attribute)

                End Select
            Else
                objNode.set_any_allowed()
            End If

            If ReferencesToResolve.Count > 0 Then
                Dim ref_cadlRefNode As openehr.openehr.am.archetype.constraint_model.ARCHETYPE_INTERNAL_REF
                Dim path As openehr.common_libs.structures.object_graph.path.OG_PATH

                For Each ref As ReferenceToResolve In ReferencesToResolve

                    path = GetPathOfNode(ref.Element.NodeId)
                    If Not path Is Nothing Then
                        ref_cadlRefNode = mCADL_Factory.create_archetype_internal_ref(ref.Attribute, openehr.base.kernel.Create.STRING.make_from_cil("ELEMENT"), path.as_string)
                    End If

                Next
                ReferencesToResolve.Clear()
            End If

        End Sub

        Protected Sub BuildSubjectOfData(ByVal subject As RelatedParty, ByVal root_node As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            If subject.Relationship.Codes.Count = 0 Then
                Return
            Else
                Dim objnode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
                Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
                Dim a_relationship As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

                an_attribute = mCADL_Factory.create_c_attribute_single(root_node, openehr.base.kernel.Create.STRING.make_from_cil("subject"))
                objnode = openehr.openehr.am.archetype.constraint_model.Create.C_COMPLEX_OBJECT.make_anonymous(openehr.base.kernel.Create.STRING.make_from_cil("RELATED_PARTY"))
                an_attribute.put_child(objnode)
                a_relationship = mCADL_Factory.create_c_attribute_single(objnode, openehr.base.kernel.Create.STRING.make_from_cil("relationship"))
                BuildCodedText(a_relationship, subject.Relationship)
            End If
        End Sub

        Protected Sub BuildSection(ByVal rmChildren As Children, ByVal cadlObj As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            ' Build a section, runtimename is already done
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

            an_attribute = mCADL_Factory.create_c_attribute_multiple(cadlObj, openehr.base.kernel.Create.STRING.make_from_cil("items"), MakeCardinality(rmChildren.Cardinality, rmChildren.Cardinality.Ordered))

            For Each a_structure As RmStructure In rmChildren

                If a_structure.Type = StructureType.SECTION Then
                    Dim new_section As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                    new_section = openehr.openehr.am.archetype.constraint_model.Create.C_COMPLEX_OBJECT.make_identified(openehr.base.kernel.Create.STRING.make_from_cil("SECTION"), openehr.base.kernel.Create.STRING.make_from_cil(a_structure.NodeId))
                    new_section.set_occurrences(MakeOccurrences(a_structure.Occurrences))

                    If a_structure.HasNameConstraint Then
                        Dim another_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
                        another_attribute = mCADL_Factory.create_c_attribute_single(new_section, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                        BuildText(another_attribute, a_structure.NameConstraint)
                    End If

                    If CType(a_structure, RmSection).Children.Count > 0 Then
                        BuildSection(CType(a_structure, RmSection).Children, new_section)
                    Else
                        new_section.set_any_allowed()
                    End If
                    an_attribute.put_child(new_section)
                ElseIf a_structure.Type = StructureType.Slot Then
                    BuildSlot(an_attribute, a_structure)
                Else
                    Debug.Assert(False)
                End If
            Next
        End Sub

        Private Sub BuildComposition(ByVal Rm As RmComposition, ByVal CadlObj As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

            ' set the category
            an_attribute = mCADL_Factory.create_c_attribute_single(CadlObj, openehr.base.kernel.Create.STRING.make_from_cil("category"))
            Dim t As New Constraint_Text
            t.TypeOfTextConstraint = TextConstrainType.Terminology ' coded_text
            t.AllowableValues.TerminologyID = "openehr"

            If Rm.IsPersistent Then
                t.AllowableValues.Codes.Add("431") ' persistent
            Else
                t.AllowableValues.Codes.Add("433") ' event
            End If

            BuildCodedText(an_attribute, t.AllowableValues)

            ' Deal with the content and context
            If Rm.Data.Count > 0 Then

                For Each a_structure As RmStructure In Rm.Data
                    Select Case a_structure.Type
                        Case StructureType.List, StructureType.Single, StructureType.Table, StructureType.Tree

                            Dim new_structure As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                            an_attribute = mCADL_Factory.create_c_attribute_single(CadlObj, openehr.base.kernel.Create.STRING.make_from_cil("context"))
                            new_structure = mCADL_Factory.create_c_complex_object_anonymous(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil("EVENT_CONTEXT"))
                            an_attribute = mCADL_Factory.create_c_attribute_single(new_structure, openehr.base.kernel.Create.STRING.make_from_cil("other_context"))
                            new_structure = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(a_structure.Type)), openehr.base.kernel.Create.STRING.make_from_cil(a_structure.NodeId))
                            BuildStructure(a_structure, new_structure)

                        Case StructureType.SECTION
                            Dim new_section As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                            If CType(a_structure, RmSection).Children.Count > 0 Then

                                an_attribute = mCADL_Factory.create_c_attribute_single(CadlObj, openehr.base.kernel.Create.STRING.make_from_cil("content"))

                                For Each slot As RmSlot In CType(a_structure, RmSection).Children

                                    BuildSlot(an_attribute, slot)
                                Next

                            End If

                        Case Else
                            Debug.Assert(False)
                    End Select
                Next
            Else
                CadlObj.set_any_allowed()
            End If
        End Sub

        Protected Sub BuildRootSection(ByVal Rm As RmSection, ByVal CadlObj As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            ' Build a section, runtimename is already done
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

            ' CadlObj.SetObjectId(openehr.base.kernel.Create.STRING.make_from_cil(Rm.NodeId))

            If Rm.Children.Count > 0 Then
                an_attribute = mCADL_Factory.create_c_attribute_multiple(CadlObj, openehr.base.kernel.Create.STRING.make_from_cil("items"), MakeCardinality(Rm.Children.Cardinality, Rm.Children.Cardinality.Ordered))

                For Each a_structure As RmStructure In Rm.Children
                    If a_structure.Type = StructureType.SECTION Then
                        Dim new_section As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                        new_section = openehr.openehr.am.archetype.constraint_model.Create.C_COMPLEX_OBJECT.make_identified(openehr.base.kernel.Create.STRING.make_from_cil("SECTION"), openehr.base.kernel.Create.STRING.make_from_cil(a_structure.NodeId))
                        new_section.set_occurrences(MakeOccurrences(a_structure.Occurrences))

                        If a_structure.HasNameConstraint Then
                            Dim another_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
                            another_attribute = mCADL_Factory.create_c_attribute_single(new_section, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                            BuildText(another_attribute, a_structure.NameConstraint)
                        End If

                        If CType(a_structure, RmSection).Children.Count > 0 Then
                            BuildSection(CType(a_structure, RmSection).Children, new_section)
                        Else
                            new_section.set_any_allowed()
                        End If
                        an_attribute.put_child(new_section)
                    ElseIf a_structure.Type = StructureType.Slot Then
                        BuildSlot(an_attribute, a_structure)
                    Else
                        Debug.Assert(False)
                    End If
                Next
            Else
                CadlObj.set_any_allowed()
            End If
        End Sub

        Private Sub BuildStructure(ByVal rm As RmStructureCompound, _
                ByVal an_adlArchetype As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT, _
                ByVal attribute_name As String)
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

            an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil(attribute_name))

            If CType(rm.Children.items(0), RmStructure).Type = StructureType.Slot Then
                BuildSlot(an_attribute, rm.Children.items(0))
            Else
                Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(rm.Children.items(0).Type)), openehr.base.kernel.Create.STRING.make_from_cil(rm.Children.items(0).NodeId))
                BuildStructure(rm.Children.items(0), objNode)
            End If
        End Sub

        Private Sub BuildProtocol(ByVal rm As RmStructure, ByVal an_adlArchetype As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim rmStructComp As RmStructureCompound

            If rm.Type = StructureType.Slot Then
                an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("protocol"))
                BuildSlot(an_attribute, rm)
            Else
                rmStructComp = CType(rm, RmStructureCompound)
                If rmStructComp.Children.Count > 0 Then
                    an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("protocol"))
                    ' only 1 protocol allowed
                    Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                    objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(rmStructComp.Children.items(0).Type)), openehr.base.kernel.Create.STRING.make_from_cil(rmStructComp.Children.items(0).NodeId))
                    BuildStructure(rmStructComp.Children.items(0), objNode)
                End If
            End If

        End Sub

        Private Sub BuildWorkFlowStep(ByVal rm As RmPathwayStep, ByVal an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE)
            Dim a_state, a_step As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim code_phrase As New CodePhrase

            objNode = mCADL_Factory.create_c_complex_object_anonymous(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil("ISM_TRANSITION"))
            a_state = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("current_state"))
            code_phrase.TerminologyID = "openehr"
            code_phrase.Codes.Add((CInt(rm.StateType)).ToString)
            If rm.HasAlternativeState Then
                code_phrase.Codes.Add(CInt(rm.AlternativeState).ToString)
            End If
            BuildCodedText(a_state, code_phrase)

            a_step = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("careflow_step"))
            code_phrase = New CodePhrase
            code_phrase.Codes.Add(rm.NodeId)  ' local is default terminology, node_id of rm is same as term code of name
            BuildCodedText(a_step, code_phrase)

        End Sub

        Private Sub BuildPathway(ByVal rm As RmStructureCompound, ByVal arch_def As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE

            If rm.Children.Count > 0 Then
                an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("ism_transition"))

                For Each pathway_step As RmPathwayStep In rm.Children
                    BuildWorkFlowStep(pathway_step, an_attribute)
                Next
            End If
        End Sub

        Private Sub BuildActivity(ByVal rm As RmActivity, ByVal an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE)
            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
            Dim objNodeSimple As openehr.openehr.am.archetype.constraint_model.C_PRIMITIVE_OBJECT

            objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil("ACTIVITY"), openehr.base.kernel.Create.STRING.make_from_cil(rm.NodeId))
            objNode.set_occurrences(MakeOccurrences(rm.Occurrences))

            If rm.ArchetypeId <> "" Then
                an_attribute = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("action_archetype_id"))
                objNodeSimple = mCADL_Factory.create_c_primitive_object(an_attribute, _
                    mCADL_Factory.create_c_string_make_from_string(openehr.base.kernel.Create.STRING.make_from_cil("/" + rm.ArchetypeId + "/")))
            End If

            For Each rm_struct As RmStructure In rm.Children
                an_attribute = mCADL_Factory.create_c_attribute_single(objNode, openehr.base.kernel.Create.STRING.make_from_cil("description"))
                Select Case rm_struct.Type
                    Case StructureType.List, StructureType.Single, StructureType.Tree, StructureType.Table
                        Dim EIF_struct As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
                        EIF_struct = mCADL_Factory.create_c_complex_object_identified(an_attribute, _
                            openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(rm_struct.Type)), _
                            openehr.base.kernel.Create.STRING.make_from_cil(rm_struct.NodeId))

                        BuildStructure(CType(rm_struct, RmStructureCompound), EIF_struct)

                    Case StructureType.Slot
                        ' this allows a structure to be archetyped at this point
                        Debug.Assert(CType(rm_struct, RmStructure).Type = StructureType.Slot)
                        BuildSlot(an_attribute, rm_struct)
                End Select
            Next

        End Sub

        Private Sub BuildInstruction(ByVal data As RmChildren)
            For Each rm As RmStructureCompound In data
                Select Case rm.Type
                    Case StructureType.Activities

                        'ToDo: Set cardinality on this attribute
                        Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
                        an_attribute = mCADL_Factory.create_c_attribute_multiple(adlArchetype.definition, _
                            openehr.base.kernel.Create.STRING.make_from_cil("activities"), _
                            MakeCardinality(New RmCardinality(0)))

                        ' only one activity allowed at present
                        Debug.Assert(rm.Children.Count < 2)

                        For Each activity As RmActivity In rm.Children
                            BuildActivity(activity, an_attribute)
                        Next
                    Case StructureType.Protocol
                        BuildProtocol(rm, adlArchetype.definition)
                    Case Else
                        Debug.Assert(False, rm.Type.ToString() & " - Type under INSTRUCTION not handled")
                End Select
            Next
        End Sub

        Private Sub BuildAction(ByVal rm As RmStructureCompound, ByVal a_definition As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT)
            Dim action_spec As RmStructure
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

            If rm.Children.items.Length > 0 Then
                an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("description"))
                action_spec = rm.Children.items(0)

                Select Case action_spec.Type
                    Case StructureType.Single, StructureType.List, StructureType.Tree, StructureType.Table
                        objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, _
                            openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(action_spec.Type)), _
                            openehr.base.kernel.Create.STRING.make_from_cil(rm.Children.items(0).NodeId))

                        BuildStructure(action_spec, objNode)

                    Case StructureType.Slot
                        ' allows action to be specified in another archetype
                        Dim slot As RmSlot = CType(action_spec, RmSlot)

                        BuildSlot(an_attribute, slot)
                End Select
            End If
        End Sub

        Public Overridable Sub MakeParseTree()
            Dim rm As RmStructureCompound
            Dim an_attribute As openehr.openehr.am.archetype.constraint_model.C_ATTRIBUTE
            Dim definition As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

            'reset the ADL definition to make it again
            adlArchetype.reset_definition()

            'pick up the description data
            adlArchetype.set_description(CType(mDescription, ADL_Description).ADL_Description)

            If cDefinition Is Nothing Then
                Err.Raise(vbObjectError + 512, "No archetype definition", _
                "An archetype definition is required prior to saving")
            End If

            mCADL_Factory = adlEngine.constraint_model_factory

            If cDefinition.hasNameConstraint Then
                an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("name"))
                BuildText(an_attribute, cDefinition.NameConstraint)
            End If


            Debug.Assert(ReferenceModel.Instance.IsValidArchetypeDefinition(cDefinition.Type))

            Select Case cDefinition.Type

                Case StructureType.Single, StructureType.List, StructureType.Tree, StructureType.Table
                    If adlArchetype.definition.any_allowed AndAlso cDefinition.Data.Count > 0 Then
                        'This can arise if the archetype has been saved with no children then
                        'items have been added later - this is percular to Tree, List and Table.
                        adlArchetype.definition.set_occurrences(MakeOccurrences(New RmCardinality(0)))
                    End If
                    BuildStructure(cDefinition, adlArchetype.definition)

                Case StructureType.SECTION
                    BuildRootSection(cDefinition, adlArchetype.definition)

                Case StructureType.COMPOSITION
                    BuildComposition(cDefinition, adlArchetype.definition)

                Case StructureType.EVALUATION, StructureType.ENTRY

                    BuildSubjectOfData(CType(cDefinition, RmEntry).SubjectOfData, adlArchetype.definition)

                    For Each rm In cDefinition.Data
                        Select Case rm.Type
                            Case StructureType.State
                                BuildStructure(rm, adlArchetype.definition, "state")

                            Case StructureType.Protocol
                                BuildProtocol(rm, adlArchetype.definition)

                            Case StructureType.Data
                                BuildStructure(rm, adlArchetype.definition, "data")

                        End Select
                    Next

                Case StructureType.ADMIN_ENTRY

                    an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("data"))
                    Try
                        Dim rm_struct As RmStructureCompound = CType(cDefinition.Data.items(0), RmStructureCompound).Children.items(0)

                        Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
                        objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(rm_struct.Type)), openehr.base.kernel.Create.STRING.make_from_cil(rm_struct.NodeId))
                        BuildStructure(rm_struct, objNode)
                    Catch
                        'ToDo - process error
                        Debug.Assert(False, "Error building structure")
                    End Try

                Case StructureType.OBSERVATION
                    BuildSubjectOfData(CType(cDefinition, RmEntry).SubjectOfData, adlArchetype.definition)

                    For Each rm In cDefinition.Data
                        Select Case rm.Type
                            Case StructureType.State
                                an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("state"))

                                'for the moment saving the state data on the first event EventSeries if there is one
                                Dim a_rm As RmStructureCompound

                                a_rm = rm.Children.items(0)

                                If a_rm.Type = StructureType.History Then
                                    BuildHistory(a_rm, an_attribute)
                                Else
                                    ' can have EventSeries for each state
                                    Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT

                                    objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(a_rm.Type)), openehr.base.kernel.Create.STRING.make_from_cil(a_rm.NodeId))
                                    BuildStructure(a_rm, objNode)
                                End If

                            Case StructureType.Protocol
                                BuildProtocol(rm, adlArchetype.definition)

                            Case StructureType.Data
                                an_attribute = mCADL_Factory.create_c_attribute_single(adlArchetype.definition, openehr.base.kernel.Create.STRING.make_from_cil("data"))

                                For Each a_rm As RmStructureCompound In rm.Children.items
                                    Select Case a_rm.Type '.TypeName
                                        Case StructureType.History
                                            BuildHistory(a_rm, an_attribute)
                                        Case Else
                                            Dim objNode As openehr.openehr.am.archetype.constraint_model.C_COMPLEX_OBJECT
                                            objNode = mCADL_Factory.create_c_complex_object_identified(an_attribute, openehr.base.kernel.Create.STRING.make_from_cil(ReferenceModel.Instance.RM_StructureName(a_rm.Type)), openehr.base.kernel.Create.STRING.make_from_cil(a_rm.NodeId))
                                            BuildStructure(a_rm, objNode)
                                    End Select
                                Next
                        End Select
                    Next

                Case StructureType.INSTRUCTION
                    BuildSubjectOfData(CType(cDefinition, RmEntry).SubjectOfData, adlArchetype.definition)

                    BuildInstruction(cDefinition.Data)

                Case StructureType.ACTION
                    BuildSubjectOfData(CType(cDefinition, RmEntry).SubjectOfData, adlArchetype.definition)

                    For Each rm In cDefinition.Data
                        Select Case rm.Type
                            Case StructureType.ISM_TRANSITION
                                BuildPathway(rm, adlArchetype.definition)
                            Case StructureType.ActivityDescription
                                BuildAction(rm, adlArchetype.definition)
                            Case StructureType.Slot
                                ' this allows a structure to be archetyped at this point
                                Debug.Assert(CType(rm.Children.items(0), RmStructure).Type = StructureType.Slot)
                                BuildStructure(rm, adlArchetype.definition)
                            Case StructureType.Protocol
                                BuildProtocol(rm, adlArchetype.definition)
                        End Select
                    Next

            End Select

        End Sub

        Sub New(ByRef an_ADL_ENGINE As openehr.adl_parser.syntax.adl.ADL_ENGINE, ByVal an_ArchetypeID As ArchetypeID, ByVal primary_language As String)
            ' call to create a brand new archetype
            MyBase.New(primary_language, an_ArchetypeID)
            adlEngine = an_ADL_ENGINE
            ' make the new archetype

            Dim id As openehr.openehr.rm.support.identification.ARCHETYPE_ID
            id = openehr.openehr.rm.support.identification.Create.ARCHETYPE_ID.make_from_string(openehr.base.kernel.Create.STRING.make_from_cil(an_ArchetypeID.ToString))
            Try
                adlEngine.create_new_archetype(id.rm_originator, id.rm_name, id.rm_entity, openehr.base.kernel.Create.STRING.make_from_cil(sPrimaryLanguageCode))
                adlArchetype = adlEngine.archetype
                adlArchetype.set_archetype_id(id)
                adlArchetype.definition.set_object_id(adlArchetype.concept_code)
            Catch
                Debug.Assert(False)
                ''FIXME raise error
            End Try
            mDescription = New ADL_Description ' nothing to pass
        End Sub

        Sub New(ByRef an_Archetype As openehr.openehr.am.archetype.ARCHETYPE, ByRef an_ADL_Engine As openehr.adl_parser.syntax.adl.ADL_ENGINE, ByVal a_filemanager As FileManagerLocal)
            ' call to create an in memory archetype from the ADL parser
            MyBase.New(an_Archetype.ontology.primary_language.to_cil)

            adlArchetype = an_Archetype
            adlEngine = an_ADL_Engine
            mArchetypeID = New ArchetypeID(an_Archetype.archetype_id.as_string.to_cil)
            ReferenceModel.Instance.ArchetypedClass = mArchetypeID.ReferenceModelEntity

            ' get the parent ID
            If Not an_Archetype.parent_archetype_id Is Nothing Then
                sParentArchetypeID = an_Archetype.parent_archetype_id.as_string.to_cil
            End If

            mDescription = New ADL_Description(adlArchetype.description)

            Select Case mArchetypeID.ReferenceModelEntity
                Case StructureType.COMPOSITION
                    cDefinition = New ADL_COMPOSITION(an_Archetype.definition, a_filemanager)
                Case StructureType.SECTION
                    cDefinition = New ADL_SECTION(an_Archetype.definition, a_filemanager)
                Case StructureType.List, StructureType.Tree, StructureType.Single
                    cDefinition = New RmStructureCompound(an_Archetype.definition, a_filemanager)
                Case StructureType.Table
                    cDefinition = New RmTable(an_Archetype.definition, a_filemanager)
                Case StructureType.ENTRY, StructureType.OBSERVATION, StructureType.EVALUATION, StructureType.INSTRUCTION, StructureType.ADMIN_ENTRY, StructureType.ACTION
                    cDefinition = New ADL_ENTRY(an_Archetype.definition, a_filemanager)
                Case Else
                    Debug.Assert(False)
            End Select

            'get the bit with the life cycle version - not possible at the moment
            Dim y() As String
            y = an_Archetype.archetype_id.as_string.to_cil.Split(".")
            If y.Length > 2 Then
                Dim i As Integer
                For i = 2 To y.Length - 1
                    sLifeCycle = sLifeCycle & y(i)
                Next
            End If

        End Sub

        Protected Sub New(ByVal primary_language As String)
            MyBase.New(primary_language)
        End Sub

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
'The Original Code is ADL_Archetype.vb.
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
