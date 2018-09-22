
type DaycountConvention = 
    | DC30E360
    | DC30360US
    | DCACT360
    | DCACT365
    | DCACTACTISDA
    | DCBUS252





type Days = Days of int with
        static member (+) (Days d1, Days d2) = d1 + d2


let d3 = Days 3



let d4 = Days 4


d3 + d4