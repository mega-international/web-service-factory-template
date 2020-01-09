'To start a macro from a web service, the macro must implement the following "Generate" method.
'Then you need to deploy it in Hopex and to start it fom the web service by passing its hexa IdAbs.
Sub Generate(megaRoot, context, data, result)
  Dim person
  Set person = megaRoot.CurrentEnvironment.GetMacro("~wjLYgZv9B5Q0[getobjectfromjson]").getobjectFromJSON(data)
  result = "Hello " & person.FirstName & " " & person.LastName
End Sub

Sub MainRoot(megaRoot)
  Dim data
  data = "{ " & """FirstName"":" & """John""" & ", " & """LastName"":" & """Doe""" & " }"
	Dim result
	Generate megaRoot, Nothing, data, result  
	megaRoot.Print result
End Sub