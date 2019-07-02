# How to Build / Run

## Things you'll need

* Visual Studio 2017+ 
* Net Core SDK
* Art Assets
* Discord Bot Token

### Assets

```
Assets/
  Backgrounds/
    Background1.png
    Background2.png
    ...
  Boss/
    BossCharacter1.png
    BossCharacter2.png
    ...
  Characters/
    Enemy1.png
    Enemy2.png
    ...
  Misc/
    Bonfire/
      frame0.png
      frame1.png
      ...
```

### How the assets are used

ImageGenerator takes the path of an enemy, and background.
We then generate a new image (720x720px), apply the background to it, and apply the character on top of it. 
This is why it's pretty important for the characters to be PNG with transparency.

The Bonfire assets are being used as follows
When there's a bonfire event; we take the image generated with just the background, lower the brightness, and render the bonfire frame on-top of it.

This will then get exported as a .gif (each png being a single frame) 



## Config Files
You'll need a config.json file next to your executable
```json
{
  "Token": "my-discord-token",
  "DiscordbotToken": "my-discordbot.org-token",
  "Guild": 01234567, 
  "CacheChannel": 01234567
}
```
where `Guild` is the ID of the guild where you'd want to post the images generated
and `CacheChannel` is the ID of the channel where you'd want to post the images generated

If `Guild` or `CacheChannel` are missing, *or* they're incorrect, the bot will not function properly.

## F.A.Q.

* It's ratelimiting me constantly!
> Yeah, so are we. This will get better with future updates (which will happen after discord has called out their hackweek winners)

* Why am I not seeing any images?
> Make sure the Assets folder is setup properly.. See above

* Why is this still so bare-bones?
> We had less than 5 days. Give us a break :V 

* Why aren't there many F.A.Q. questions? 
> I can't think of any right now. Make an issue if you have any issues, we'll most likely add it to the F.A.Q. if it's important 👏

