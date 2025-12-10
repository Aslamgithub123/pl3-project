module simpleStore.UI

open System
open System.Windows.Forms
open System.Drawing
open Domain
open Services

let createMainWindow (initialState: StoreState) =
    let form = new Form(Text = "F# Simple Store", Width = 1000, Height = 700)
    
    // Mutable state to track the UI
    let mutable currentState = initialState
    let mutable currentDisplayedProducts = initialState.Catalog |> Map.toList |> List.map snd

    // --- CONTROLS ---

    // 1. Search & Filter Panel
    let panelTop = new FlowLayoutPanel(Dock = DockStyle.Top, Height = 60, Padding = Padding(10))
    
    let lblSearch = new Label(Text = "Search:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight)
    let txtSearch = new TextBox(Width = 200)
    let btnSearch = new Button(Text = "Go", Width = 60)
    
    let lblCategory = new Label(Text = "Category:", AutoSize = true, TextAlign = ContentAlignment.MiddleRight)
    let cmbCategory = new ComboBox(DropDownStyle = ComboBoxStyle.DropDownList, Width = 150)
    
    // Add "All" option + categories from the loaded data
    cmbCategory.Items.Add("All Categories")
    let categories = Catalog.getCategories initialState.Catalog
    categories |> List.iter (fun c -> cmbCategory.Items.Add(c) |> ignore)
    cmbCategory.SelectedIndex <- 0 // Default to "All"

    let btnReset = new Button(Text = "Reset", Width = 80)

    // 2. Products List (Left Side)
    let splitContainer = new SplitContainer(Dock = DockStyle.Fill, SplitterDistance = 600)
    
    let lstProducts = new ListView(Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true)
    lstProducts.Columns.Add("ID", 40) |> ignore
    lstProducts.Columns.Add("Name", 200) |> ignore
    lstProducts.Columns.Add("Category", 100) |> ignore
    lstProducts.Columns.Add("Price", 80) |> ignore

    let btnAddToCart = new Button(Text = "Add to Cart", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.LightGreen)

    // 3. Cart List (Right Side)
    let lstCart = new ListView(Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true)
    lstCart.Columns.Add("Product", 150) |> ignore
    lstCart.Columns.Add("Qty", 50) |> ignore
    lstCart.Columns.Add("Price", 80) |> ignore

    let panelCartBottom = new Panel(Dock = DockStyle.Bottom, Height = 100)
    let lblTotal = new Label(Text = "Total: $0.00", Font = new Font("Arial", 14.0f, FontStyle.Bold), AutoSize = true, Location = Point(10, 10))
    let btnRemove = new Button(Text = "Remove Selected", Width = 120, Location = Point(10, 50))
    let btnCheckout = new Button(Text = "Checkout", Width = 120, Location = Point(140, 50), BackColor = Color.Orange)

    // --- HELPER FUNCTIONS ---

    let renderProducts products =
        lstProducts.Items.Clear()
        products |> List.iter (fun (p: Product) ->
            let item = new ListViewItem(p.Id.ToString())
            item.SubItems.Add(p.Name) |> ignore
            item.SubItems.Add(p.Category) |> ignore
            item.SubItems.Add(sprintf "$%.2f" p.Price) |> ignore
            item.Tag <- p
            lstProducts.Items.Add(item) |> ignore
        )

    let renderCart cart =
        lstCart.Items.Clear()
        cart |> List.iter (fun (item: CartItem) ->
            let lvi = new ListViewItem(item.Product.Name)
            lvi.SubItems.Add(item.Quantity.ToString()) |> ignore
            lvi.SubItems.Add(sprintf "$%.2f" (item.Product.Price * decimal item.Quantity)) |> ignore
            lvi.Tag <- item.Product.Id
            lstCart.Items.Add(lvi) |> ignore
        )
        let total = Pricing.calculateTotal cart
        lblTotal.Text <- sprintf "Total: $%.2f" total

    // --- EVENT HANDLERS ---

    // Filter Logic
    let applyFilters () =
        let searchText = txtSearch.Text
        let selectedCategory = cmbCategory.SelectedItem.ToString()

        // Start with full catalog
        let allProducts = initialState.Catalog 
        
        // 1. Apply Category Filter
        let afterCategory = 
            if selectedCategory = "All Categories" then 
                allProducts |> Map.toList |> List.map snd
            else 
                Search.filterByCategory selectedCategory allProducts

        // 2. Apply Search Filter
        let finalResult = 
            if String.IsNullOrWhiteSpace searchText then 
                afterCategory
            else 
                afterCategory |> List.filter (fun p -> 
                    p.Name.ToLower().Contains(searchText.ToLower())
                )

        currentDisplayedProducts <- finalResult
        renderProducts currentDisplayedProducts

    // Wire up events
    btnSearch.Click.Add(fun _ -> applyFilters())
    
    // Trigger filter immediately when Category is picked
    cmbCategory.SelectedIndexChanged.Add(fun _ -> applyFilters())

    btnReset.Click.Add(fun _ -> 
        txtSearch.Text <- ""
        cmbCategory.SelectedIndex <- 0
        applyFilters()
    )

    btnAddToCart.Click.Add(fun _ -> 
        if lstProducts.SelectedItems.Count > 0 then
            let product = lstProducts.SelectedItems.[0].Tag :?> Product
            let newCart = Cart.addToCart product currentState.Cart
            
            // Update State
            currentState <- { currentState with Cart = newCart }
            
            // Save & Render
            FileIO.saveCart newCart
            renderCart newCart
        else
            MessageBox.Show("Please select a product first.") |> ignore
    )

    btnRemove.Click.Add(fun _ ->
        if lstCart.SelectedItems.Count > 0 then
            let productId = lstCart.SelectedItems.[0].Tag :?> int
            let newCart = Cart.removeFromCart productId currentState.Cart
            currentState <- { currentState with Cart = newCart }
            FileIO.saveCart newCart
            renderCart newCart
    )

    btnCheckout.Click.Add(fun _ ->
        if currentState.Cart.IsEmpty then
            MessageBox.Show("Cart is empty!") |> ignore
        else
            let total = Pricing.calculateTotal currentState.Cart
            FileIO.saveReceipt currentState.Cart total
            MessageBox.Show(sprintf "Receipt saved! Total paid: $%.2f" total) |> ignore
            
            // Clear Cart
            currentState <- { currentState with Cart = [] }
            FileIO.saveCart []
            renderCart []
    )

    // --- LAYOUT ASSEMBLY ---
    
    // FIX APPLIED HERE: Upcasting elements to Control
    panelTop.Controls.AddRange([| 
        lblSearch :> Control
        txtSearch :> Control
        btnSearch :> Control
        lblCategory :> Control
        cmbCategory :> Control
        btnReset :> Control 
    |])
    
    splitContainer.Panel1.Controls.Add(lstProducts)
    splitContainer.Panel1.Controls.Add(btnAddToCart)
    
    // FIX APPLIED HERE: Upcasting elements to Control
    panelCartBottom.Controls.AddRange([| 
        lblTotal :> Control
        btnRemove :> Control
        btnCheckout :> Control 
    |])

    splitContainer.Panel2.Controls.Add(lstCart)
    splitContainer.Panel2.Controls.Add(panelCartBottom)

    form.Controls.Add(splitContainer)
    form.Controls.Add(panelTop)

    // Initial Render
    renderProducts currentDisplayedProducts
    renderCart currentState.Cart

    Application.Run(form)