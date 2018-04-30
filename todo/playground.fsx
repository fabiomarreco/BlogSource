//#load @"c:\packages.fsx"

type Undefined = Exception
open System.Web.Security
open System.Runtime.Remoting.Metadata.W3cXsd2001
//...


module InterestRate = 
    type DaycountConvention = 
        | DC30E360
        | DC30360US
        | DCACT360
        | DCACT365
        | DCACTACTISDA
        | DCBUS252

    type CompoundFrequency = 
        | Annually
        | Monthly
        | Daily
        | Continous

    type Compound = 
        | Simple
        | Compounded of CompoundFrequency

    type InterestRate = {
        Rate : decimal
        Compound: Compound;
        Daycount : DaycountConvention
    }

    let create daycount compound rate =  {
        Rate = rate;
        Compound = compound;
        Daycount = daycount
    }


    let treasury = create DCACTACTISDA Simple

    let cdi = create DCBUS252 (Compounded Annually)


InterestRate.cdi 0.12M

InterestRate.treasury 0.2M