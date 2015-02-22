﻿module internal FunScript.LetBindings

open AST
open Microsoft.FSharp.Quotations

let private binding =
   CompilerComponent.create <| fun (|Split|) compiler returnStrategy ->
      let (|Return|) = compiler.Compile
      function
      | Patterns.Let(var, Split(valDecl, valRef), Return returnStrategy block) ->
         [ yield! valDecl
           yield DeclareAndAssign(var, valRef)
           yield! block
         ]
      | _ -> []

let private recBinding =
   CompilerComponent.create <| fun (|Split|) compiler returnStrategy ->
      let (|Return|) = compiler.Compile
      function
      | Patterns.LetRecursive(bindingExprs, Return returnStrategy block) ->
         [ yield! bindingExprs |> List.map (fun (var, _) -> Declare [var])
           yield! 
               bindingExprs 
               |> Seq.collect (fun (var, Split(valDecl, valRef)) ->
                  seq {
                     yield! valDecl
                     yield Assign(Reference var, valRef)
                  })                           
           yield! block
         ]
      | _ -> []

let private reference =
   CompilerComponent.create <| fun (|Split|) compiler returnStrategy ->
      function
      | Patterns.Var(var) -> [ yield returnStrategy.Return <| Reference var ]
      | _ -> []

let private mutation =
   CompilerComponent.create <| fun (|Split|) compiler returnStrategy ->
      function
      | Patterns.VarSet(var, Split(valDecl, valRef)) -> 
         [  yield! valDecl
            yield Assign(Reference var, valRef)
            if returnStrategy = ReturnStrategies.inplace then
               yield returnStrategy.Return Null 
         ]
      | _ -> []

let components = [ 
   binding
   recBinding
   reference
   mutation
]