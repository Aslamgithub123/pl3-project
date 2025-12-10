module simpleStore.Services

open System
open System.IO
open System.Text.Json
open Domain 

module Catalog =
    // 1. Define the path to your data file
    let private dataPath = "products.json"

    // 2. Function to load products from the JSON file
    let private loadProductsFromFile () =
        try
            if File.Exists(dataPath) then
                let json = File.ReadAllText(dataPath)
                
                // Handle empty file case
                if String.IsNullOrWhiteSpace(json) then 
                    []
                else 
                    // Case-insensitive deserialization is usually safer
                    let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
                    JsonSerializer.Deserialize<Product list>(json, options)
            else
                // If file doesn't exist, return empty list (or you could log a warning)
                printfn "Warning: %s not found. Starting with empty catalog." dataPath
                []
        with
        | ex -> 
            printfn "CRITICAL ERROR: Could not load products. Details: %s" ex.Message
            []

    // 3. Initialize the catalog by reading the file
    let initCatalog () =
        loadProductsFromFile ()
        |> List.map (fun p -> (p.Id, p))
        |> Map.ofList

    let getProduct id catalog = Map.tryFind id catalog

    // 4. Helper to get a unique list of categories (Useful for filling a UI dropdown)
    let getCategories (catalog: Map<int, Product>) =
        catalog
        |> Map.toList
        |> List.map (fun (_, p) -> p.Category)
        |> List.distinct
        |> List.sort

module Cart =
    let addToCart (product: Product) (cart: CartItem list) =
        match cart |> List.tryFind (fun item -> item.Product.Id = product.Id) with
        | Some existingItem ->
            let newItem = { existingItem with Quantity = existingItem.Quantity + 1 }
            cart |> List.map (fun item -> if item.Product.Id = product.Id then newItem else item)
        | None ->
            { Product = product; Quantity = 1 } :: cart

    let removeFromCart (productId: int) (cart: CartItem list) =
        match cart |> List.tryFind (fun item -> item.Product.Id = productId) with
        | Some item when item.Quantity > 1 ->
            let newItem = { item with Quantity = item.Quantity - 1 }
            cart |> List.map (fun i -> if i.Product.Id = productId then newItem else i)
        | Some _ ->
            cart |> List.filter (fun item -> item.Product.Id <> productId)
        | None -> cart

module Pricing = 
    let calculateTotal (cart: CartItem list) =
        cart |> List.sumBy (fun item -> item.Product.Price * decimal item.Quantity)

module Search =
    // 5. NEW: Exact filter for Category
    let filterByCategory (category: string) (catalog: Map<int, Product>) =
        catalog
        |> Map.toList
        |> List.map snd // Convert (id, product) tuple to just product
        |> List.filter (fun p -> p.Category = category)

    // 6. IMPROVED: Search is now cleaner and safer
    let searchProducts (query: string) (catalog: Map<int, Product>) =
        let normalizedQuery = query.ToLower().Trim()
        
        catalog
        |> Map.toList
        |> List.map snd
        |> List.filter (fun p -> 
            p.Name.ToLower().Contains(normalizedQuery) || 
            p.Category.ToLower().Contains(normalizedQuery)
        )

module FileIO =
    type Receipt = { Date: DateTime; Items: CartItem list; Total: decimal }

    // Existing Receipt logic
    let saveReceipt (cart: CartItem list) (total: decimal) =
        let filePath = "receipts.json"
        let newReceipt = { Date = DateTime.Now; Items = cart; Total = total }
        let options = JsonSerializerOptions(WriteIndented = true)

        let existingReceipts = 
            if File.Exists(filePath) then
                let content = File.ReadAllText(filePath)
                if String.IsNullOrWhiteSpace(content) then [] 
                else 
                    try JsonSerializer.Deserialize<Receipt list>(content)
                    with _ -> []
            else []

        let updatedReceipts = existingReceipts @ [newReceipt]
        let json = JsonSerializer.Serialize(updatedReceipts, options)
        File.WriteAllText(filePath, json)
    
    // Existing Cart logic
    let loadCart () = 
        let filePath = "cart.json"
        try
            if File.Exists(filePath) then
                let jsonString = File.ReadAllText(filePath)
                if String.IsNullOrWhiteSpace(jsonString) then []
                else JsonSerializer.Deserialize<CartItem list>(jsonString)
            else [] 
        with
        | ex -> 
            printfn "Error loading cart: %s" ex.Message
            []

    let saveCart (cart: CartItem list) =
        let filePath = "cart.json"
        try
            let options = JsonSerializerOptions(WriteIndented = true)
            let jsonString = JsonSerializer.Serialize(cart, options)
            File.WriteAllText(filePath, jsonString)
        with
        | ex -> printfn "Error saving cart: %s" ex.Message