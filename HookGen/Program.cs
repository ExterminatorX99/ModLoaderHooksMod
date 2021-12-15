using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour.HookGen;

namespace HookGen;

public static class Program
{
	private const string installedNetRefs = @"\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.0\ref\net6.0";
	private const string tmlAssemblyPath = @"E:\Games\Steam\steamapps\common\tModLoader\tModLoader.dll"; // hardcode
	private static readonly string binLibsPath = $"{Directory.GetParent(tmlAssemblyPath)}/Libraries";

	public static void Main(string[] args)
	{
		const string outputPath = "ModLoaderHooks.dll";
		if (File.Exists(outputPath))
			File.Delete(outputPath);

		HookGen(tmlAssemblyPath, outputPath);

		//File.Delete(Path.ChangeExtension(outputPath, "pdb"));

		Directory.CreateDirectory("../../../../lib/");
		File.Copy(outputPath, $"../../../../lib/{outputPath}");
	}

	private static void HookGen(string inputPath, string outputPath)
	{
		using var mm = new MonoModder
		{
			InputPath = inputPath,
			OutputPath = outputPath,
			ReadingMode = ReadingMode.Deferred,
			DependencyDirs =
			{
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + installedNetRefs,
			},
			MissingDependencyThrow = false,
		};

		mm.DependencyDirs.AddRange(Directory.GetDirectories(binLibsPath, "*", SearchOption.AllDirectories));

		mm.Read();
		mm.MapDependencies();

		var gen = new HookGenerator(mm, "ModLoaderHooks")
		{
			HookPrivate = true,
		};
		gen.Generate();
		RemoveNonModLoaderTypes(gen.OutputModule);
		gen.OutputModule.Write(outputPath);
	}

	private static void RemoveNonModLoaderTypes(ModuleDefinition module)
	{
		for (int i = module.Types.Count - 1; i >= 0; i--)
			if (!module.Types[i].FullName.Contains("Terraria.ModLoader"))
				module.Types.RemoveAt(i);
	}
}
