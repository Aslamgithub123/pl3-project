module simpleStore.Domain

type Product = {
    Id: int
    Name: string
    Price: decimal
    Category: string
}

type CartItem = {
    Product: Product
    Quantity: int
}

type StoreState = {
    Catalog: Map<int, Product>
    Cart: CartItem list
}