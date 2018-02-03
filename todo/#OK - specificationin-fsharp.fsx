
type Undefined = Exception

type ProductId = ProductId of string
type ProductCategory = 
    | Mobile
    | Kids
    | Tv

type Product = {Id : ProductId; Name : string; Category : ProductCategory}

type ProductSpecification = 
    | WithId of ProductId
    | ProductNameContains of string
    | InCategory of ProductCategory
    | Not of ProductSpecification
    | And of (ProductSpecification * ProductSpecification)
    | Or of (ProductSpecification * ProductSpecification)

let products =  [
        { Id = ProductId "ID1"; Name = "Toy aa1"; Category = Kids };
        { Id = ProductId "ID2"; Name = "Samsung SI4"; Category = Mobile };
        { Id = ProductId "ID3"; Name = "Samsung TV"; Category = Tv };
        { Id = ProductId "ID4"; Name = "IPod 5"; Category = Mobile }
    ]


let rec isSatisfyedby spec product  =
    match spec with
    | WithId p -> product.Id = p
    | ProductNameContains pn -> product.Name.Contains(pn)
    | InCategory cat -> product.Category = cat
    | And (left, right) -> isSatisfyedby left product && isSatisfyedby right product
    | Or (left, right) -> isSatisfyedby  left product || isSatisfyedby right product
    | Not (spec) -> not (isSatisfyedby   spec product)

let inline (<&>) (left:ProductSpecification) right = And (left, right)
let inline (<|>) (left:ProductSpecification) right = Or (left, right)

let spec2 = ProductNameContains "Samsung" <&> InCategory Tv <|> WithId (ProductId "ID1")


products |> List.filter (isSatisfyedby spec2)

