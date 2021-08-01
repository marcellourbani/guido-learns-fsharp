(* PREPARATION

Before you start, it's worth knowing about the Elmish architecture
See the overview slide.

*)


// Task 5 - play with a new web service using F# scripting

// Task 5.1 -
//   Problem: Learn to use the F# REPL
//
//   Approach
//   - Type 1+1
//   - Select the code (keyboard or mouse)
//   - Use "Alt-Enter" to execute in REPL

1 + 1

// Task 5.2
//   Problem: we want to examine the REST service we would like to use
//
//   Approach:
//     Look at https://www.geonames.org/export/ws-overview.html
//     Look at http://api.geonames.org/findNearbyWikipediaJSON?lat=52.3676&lng=4.9041&username=dsyme



// Task 5.3
//   Problem: We need a package to use
//
//   Look at https://nuget.org, search for FSharp.Data
//   Copy the text to reference for scripting.


// Task 5.4 - Point the JsonProvider to the web service
//
#r "nuget:FSharp.Data"

open FSharp.Data

type WikipediaIO =
    JsonProvider<"https://secure.geonames.org/findNearbyWikipediaJSON?lat=52.3676&lng=4.9041&username=dsyme">

// Task 5.5 - Use the JsonProvider to download the data, the code is below
//
let info =
    $"https://secure.geonames.org/findNearbyWikipediaJSON?lat=52.3676&lng=4.9041&username=dsyme"
    |> WikipediaIO.AsyncLoad
    |> Async.RunSynchronously



// Task 5.6 - Extract the title, latitude, longitude and summary for each result
//
// A starting snippet is below

type WikiEntry =
    { Title: string
      Latitude: decimal
      Longitude: decimal
      Summary: string }

let fromJson (x: WikipediaIO.Geoname) =
    { Title = x.Title
      Latitude = x.Lat
      Longitude = x.Lng
      Summary = x.Summary }

let results = [ for x in info.Geonames -> fromJson x ]

let printResult x =
    printfn "Title: %s\nCoordinates: %f,%f\nSummary: %s\n\n" x.Title x.Latitude x.Longitude x.Summary

// results = [ for x in info.Geonames -> x.Title ]
[ for x in results -> printResult x ]
