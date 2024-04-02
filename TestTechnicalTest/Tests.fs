module Tests

open System
open TechnicalTest.Types
open Xunit
open TechnicalTest.Main

[<Fact>]
let ``generateDateOfMonthCache tests`` () =
    let dates = Set.ofList [ DateOnly.Parse("2024-01-01"); DateOnly.Parse("2024-02-01"); DateOnly.Parse("2024-01-03") ]
    let expected = Map.ofList [ (1, Set.ofList [ DateOnly.Parse("2024-01-01"); DateOnly.Parse("2024-02-01")]); (3, Set.ofList [ DateOnly.Parse("2024-01-03") ]) ]
    
    let result = generateSpecifiedDateOfMonthCache dates

    Assert.Equal<Map<int, Set<DateOnly>>>(expected, result)

[<Fact>]
let ``generateDateOfWeekCache tests`` () =
    let dates =  Set.ofList  [ DateOnly.Parse("2024-01-01"); DateOnly.Parse("2024-01-08"); DateOnly.Parse("2024-01-03") ]
    let expected = Map.ofList [ (DayOfWeek.Monday, Set.ofList [ DateOnly.Parse("2024-01-01"); DateOnly.Parse("2024-01-08")]); (DayOfWeek.Wednesday, Set.ofList [ DateOnly.Parse("2024-01-03") ]) ]
    
    let result = generateSpecifiedDayOfWeekCache dates

    Assert.Equal<Map<DayOfWeek, Set<DateOnly>>>(expected, result)

[<Fact>]
let ``generateDateSequence should correctly generate sequence of dates`` () =
    let startDate = DateOnly.Parse("2024-01-01")
    let endDate = DateOnly.Parse("2024-01-03")
    let expected = Set.ofList [ DateOnly.Parse("2024-01-01"); DateOnly.Parse("2024-01-02"); DateOnly.Parse("2024-01-03") ]

    let result = generateDateSequence startDate endDate

    Assert.Equal<Set<DateOnly>>(expected, result)

[<Fact>]
let ``createEmptyDateMap should correctly create map with empty sets`` () =
    let dates = Set.ofList [ DateOnly.Parse("2024-01-01"); DateOnly.Parse("2024-01-02") ]
    let expected = Map.ofList [ (DateOnly.Parse("2024-01-01"), Set.empty); (DateOnly.Parse("2024-01-02"), Set.empty) ]

    let result = createEmptyDateMap dates

    Assert.Equal< Map<DateOnly, Set<string>>>(expected, result)
    

[<Fact>]
let ``generateCustomerPreference should create correct output`` () =
    let customers =  [
                { Name = "Customer 1"; MarketingPreference = OnSpecifiedDateOfMonth(5) }
                { Name = "Customer 2"; MarketingPreference = OnSpecifiedDateOfMonth(9) }
                { Name = "Customer 3"; MarketingPreference = OnSpecifiedDayOfWeek(Set.ofList [DayOfWeek.Monday; DayOfWeek.Wednesday]) }
                { Name = "Customer 4"; MarketingPreference = EveryDay }
                { Name = "Customer 5"; MarketingPreference = Never }
            ]
    let startDate = DateOnly.Parse("2024-01-01")
    let endDate = DateOnly.Parse("2024-01-9")
    let dateRanges = generateDateSequence startDate endDate
    let expected =
        Map.ofList [
            (DateOnly.Parse("2024-01-01"), Set.ofList ["Customer 4"; "Customer 3"])
            (DateOnly.Parse("2024-01-02"), Set.ofList ["Customer 4"])
            (DateOnly.Parse("2024-01-03"), Set.ofList ["Customer 4"; "Customer 3"])
            (DateOnly.Parse("2024-01-04"), Set.ofList ["Customer 4"])
            (DateOnly.Parse("2024-01-05"), Set.ofList ["Customer 4"; "Customer 1"])
            (DateOnly.Parse("2024-01-06"), Set.ofList ["Customer 4"])
            (DateOnly.Parse("2024-01-07"), Set.ofList ["Customer 4"]) 
            (DateOnly.Parse("2024-01-08"), Set.ofList ["Customer 4"; "Customer 3"])
            (DateOnly.Parse("2024-01-09"), Set.ofList ["Customer 4"; "Customer 2"]) 
        ]

    let result = generateCustomerPreference customers dateRanges

    Assert.Equal<Map<DateOnly, Set<CustomerId>>>(expected, result)
