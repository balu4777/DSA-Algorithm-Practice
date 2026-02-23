using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class EntityModel
{
	[JsonProperty("id")]
	public long Id {get; set;}
	[JsonProperty("user_id")]
	public long UserId {get; set;}
	[JsonProperty("age")]
	public int Age {get; set;}
	[JsonProperty("user_weight")]
	public double UserWeight {get; set;}
	[JsonProperty("name")]
	public string Name {get; set;}
	[JsonProperty("price")]
	public double Price {get; set;}
	[JsonProperty("weight")]
	public double Weight {get; set;}
	[JsonProperty("calories")]
	public double Calories {get; set;}
	[JsonProperty("fat")]
	public double Fat {get; set;}
	[JsonProperty("carbs")]
	public double carbs {get;set;}
	[JsonProperty("protein")]
	public double Protein {get;set;}
	[JsonProperty("time_consumed")]
	public TimeSpan TimeConsumed {get;set;}
	[JsonProperty("date_consumed")]
	public DateTime DateConsumed {get;set;}
	[JsonProperty("type")]
	public string Types {get;set;}
	[JsonProperty("favorite")]
	public bool Favorite {get;set;}
	[JsonProperty("procedence")]
	public string Procedence {get;set;}
}

public class program
{
	private const string baseUrl = "https://git.toptal.com/screeners/calories-json/-/raw/main/calories.json";
	public async static Task Main(string[] args)
	{
		Console.WriteLine("Welcome");
		var httpClient = new HttpClient();
		var jsonObj = await httpClient.GetAsync(baseUrl);
		var body =await jsonObj.Content.ReadAsStringAsync();
		var result = JsonConvert.DeserializeObject<List<EntityModel>>(body);
		Console.Write(result.Count);
		
		//1)Total calories per user per day
		var totalCalPerUserPerDay =result.GroupBy(g => new {
			g.UserId, 
			Day = g.DateConsumed
		}).Select(s => new {
			s.Key.UserId,
			s.Key.Day,
			TotalCalories = s.Sum(x =>x.Calories)
		}).OrderBy(x => x.UserId).ThenBy(x => x.Day);
		
		foreach (var row in totalCalPerUserPerDay)
		{
			Console.WriteLine(row);
		}
		//2)Users with the most days under 1800 calories (classic screener)
		
		var under1800DaysByUser  = result.GroupBy(
		g =>new {
			g.UserId,
			g.Name,
			g.DateConsumed
		}).Select( s=> new {
			s.Key.UserId,
			s.Key.DateConsumed,
			Total = s.Sum(x => x.Calories)
		}).Where( e => e.Total <1800).GroupBy(x => x.UserId)
			.Select( s => new {s.Key, DaysUnder = s.Count()}).OrderByDescending(x =>x.DaysUnder);
		
		
		//8) Favorite items by user (favorite == "yes")
		var favoriteItems = result.Where(e => e.Favorite)
			.GroupBy(e =>new{ e.UserId ,e.Name}).Select(s => new { s.Key.Name, s.Key.UserId}).Distinct();
		
		foreach( var item in favoriteItems)
		{
			Console.WriteLine(item);
		}
	}
	
}

