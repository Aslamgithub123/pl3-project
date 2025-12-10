open simpleStore

open Domain
open Services
open UI

[<EntryPoint>]
let main argv =
    let initialState = {
        Catalog = Catalog.initCatalog()
        Cart = FileIO.loadCart()
    }
    
    printfn "Welcome to the F# Simple Store!"
    
    UI.createMainWindow initialState
    
    0