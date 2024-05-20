Before program launch:
1. Download dotnet and Visual Studio:
   1) https://visualstudio.microsoft.com/
   - https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-tutorial/install
2. Find green button 'Code' on this page, click and choose 'Open with Visual Studio'
- To open project in Visual Studio: Open Visual Studio program after that select 'Open project or solution', find your project file ('.sln') and open it.
3. After that open cmd in Autoparts folder and run these commands:
   1) dotnet tool install --global dotnet-ef
   2) dotnet ef database update
  
- To start project just press 'F5' in Visual Studio project

4. Default website administrator:
   -login: dima
   -password: auto
5. You can change it in /Context.cs
