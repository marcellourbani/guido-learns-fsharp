module Api

open DataAccess
open FSharp.Data.UnitSystems.SI.UnitNames
open Shared

let country = "NL"

#if SOLVED
let private london = { Latitude = 51.5074; Longitude = 0.1278 }
#else
let private schiphol = { Latitude = 52.3105; Longitude = 4.7683 }
#endif

let getLocationResponse postcode = async {
    if not (Validation.isValidPostcode country postcode) then failwith "Invalid postcode"

    let! location = getLocation postcode
#if SOLVED
    let distanceToAirport = getDistanceBetweenPositions location.LatLong london
#else
    let distanceToAirport = getDistanceBetweenPositions location.LatLong schiphol
#endif
    let lr = 
        { Postcode = postcode
          Location = location
          DistanceToAirport = (distanceToAirport / 1000.<meter>) }
    return lr
}

let private asWeatherResponse (weather: Weather.MetaWeatherLocation.Root) =
    { WeatherType =
        weather.ConsolidatedWeather
        |> Array.countBy(fun w -> w.WeatherStateName)
        |> Array.maxBy snd
        |> fst
        |> WeatherType.Parse
      AverageTemperature = weather.ConsolidatedWeather |> Array.averageBy(fun r -> float r.TheTemp) }

let getWeather postcode = async {
    let! loc = GeoLocation.getLocation postcode
    let! weather = Weather.getWeatherForPosition loc.LatLong
    let response = weather |> asWeatherResponse
    return response
    //async.Return { WeatherType = WeatherType.Clear; AverageTemperature = 0. }
}

let dojoApi =
    { GetLocation = getLocationResponse

      GetWeather = getWeather
    }