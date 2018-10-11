# AspNetMvcSubResourceIntegrityHelper
Sub resource integrity helper for vanilla ASP.NET MVC. (Not core).

Adds a few SRI-functions to the html helper.

```
@Html.SRICssLink("/Content/bootstrap.css")
@Html.SRIScriptLink("/Scripts/bootstrap.js")
```
which translates to
```
<link href="/Content/bootstrap.css" rel="stylesheet"  integrity="sha384-2QMA5oZ3MEXJddkHyZE/e/C1bd30ZUPdzqHrsaHMP3aGDbPA9yh77XDHXC9Imxw+" crossorigin="anonymous" />
<script src="/Scripts/bootstrap.js"  integrity="sha384-fyOlGC+soQAvVFysE2KxkXaVKf75M1Zyo6RG7thLEEwD7p6/Cso7G/iV9tPM0C/a" crossorigin="anonymous" ></script>
```

