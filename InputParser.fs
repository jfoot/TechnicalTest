module TechnicalTest.InputParser

    open System
    open System.IO
    open System.Text.Json
    open TechnicalTest.Types
           
    let parseDateRange (startString : string) : Result<DateOnly *  DateOnly, string> =
        let parseDateString (input : string) : Result<DateOnly, string> =
            match DateOnly.TryParse input with
            | true, date -> Ok date
            | false, _ ->  Error "Invalid date format"
        
        match parseDateString startString with
        | Ok startDate ->
            Ok (startDate, startDate.AddDays 90)
        | _ ->
            Error $"Date range invalid, should be in the format of dd-MM-yyyy"

    let readJsonFromFile (filePath : string) : Result<string, string> =
        try
            Ok (File.ReadAllText(filePath))
        with
        | :? FileNotFoundException ->
            Error $"File not found: %s{filePath}"
        | ex ->
            Error ex.Message

    let validateAndRemoveDuplicates customers =
        let isValidDate (date : int) =
            date >= 1 && date <= 28
        
        let isValidDay (days : Set<DayOfWeek>) =
            let minDay = DayOfWeek.Sunday
            let maxDay = DayOfWeek.Saturday
            days |> Set.forall (fun day -> day >= minDay && day <= maxDay)

        let customerValidCheck (validCustomers, names) customer =
            match customer with
            | { Name = name; MarketingPreference = OnSpecifiedDateOfMonth date } when not (isValidDate date) ->
                printfn $"Customer %s{name} has an invalid date: {date}. Removing from the list."
                (validCustomers, names)
            | { Name = name; MarketingPreference = OnSpecifiedDayOfWeek days } when not (isValidDay days) ->
                printfn $"Customer %s{name} has an invalid days of week: {days}. Removing from the list."
                (validCustomers, names)
            | { Name = name } when Set.contains name names ->
                printfn $"Duplicate customer found: %s{name}. Removing from the list."
                (validCustomers, names)
            | { Name = name } ->
                (customer :: validCustomers, Set.add name names)

        List.fold customerValidCheck ([], Set.empty) customers |> fst

    let generateCustomerData (filePath : string) : Result<List<Customer>, string> =
       match readJsonFromFile filePath with
       | Ok customJson ->
           try
              let customers = JsonSerializer.Deserialize<List<Customer>>(customJson)
              Ok (validateAndRemoveDuplicates customers)
           with
           | ex ->
                Error $"Failed deserializing JSON: {ex.Message}"
       | Error msg -> Error msg