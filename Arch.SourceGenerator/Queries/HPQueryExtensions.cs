using System.Text;

namespace ArchSourceGenerator;

public struct InterfaceInfo
{
    public string Name { get; set; }
    public List<string> Generics { get; set; }
    public List<string> Params { get; set; }
}

public static class StringBuilderHpQueryExtensions
{
    public static StringBuilder Append(this StringBuilder sb, ref InterfaceInfo interfaceInfo)
    {
        var genericSb = new StringBuilder();
        foreach (var generic in interfaceInfo.Generics)
            genericSb.Append(generic).Append(",");
        genericSb.Length--;

        var paramSb = new StringBuilder();
        foreach (var param in interfaceInfo.Params)
            paramSb.Append(param).Append(",");
        paramSb.Length--;

        var interfaceTemplate = $@"
public interface {interfaceInfo.Name}<{genericSb}>{{
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Update({paramSb});
}}
";
        sb.Append(interfaceTemplate);
        return sb;
    }

    public static StringBuilder AppendInterfaces(this StringBuilder sb, int amount)
    {
        var generics = new List<string>();
        var parameters = new List<string>();

        for (var index = 0; index <= amount; index++)
        {
            generics.Add($"T{index}");
            parameters.Add($"ref T{index} t{index}");
            var interfaceInfo = new InterfaceInfo { Name = "IForEach", Generics = generics, Params = parameters };
            sb.Append(ref interfaceInfo);
        }

        return sb;
    }

    public static StringBuilder AppendEntityInterfaces(this StringBuilder sb, int amount)
    {
        var generics = new List<string>();
        var parameters = new List<string>();

        parameters.Add("in Entity entity");

        for (var index = 0; index <= amount; index++)
        {
            generics.Add($"T{index}");
            parameters.Add($"ref T{index} t{index}");
            var interfaceInfo = new InterfaceInfo { Name = "IForEachWithEntity", Generics = generics, Params = parameters };
            sb.Append(ref interfaceInfo);
        }

        return sb;
    }

    public static void AppendQueryInterfaceMethods(this StringBuilder builder, int amount)
    {
        for (var index = 0; index <= amount; index++)
        {
            var generics = new StringBuilder().GenericWithoutBrackets(index);
            var getArrays = new StringBuilder().GetGenericArrays(index);
            var getFirstElement = new StringBuilder().GetFirstGenericElements(index);
            var getComponents = new StringBuilder().GetGenericComponents(index);
            var insertParams = new StringBuilder().InsertGenericParams(index);

            var methodTemplate = $@"
public partial class World{{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HPQuery<T,{generics}>(in QueryDescription description, ref T iForEach) where T : struct, IForEach<{generics}>{{
        
        var query = Query(in description);
        foreach (ref var chunk in query.GetChunkIterator()) {{ 

            var chunkSize = chunk.Size;
            {getArrays}
            
            {getFirstElement}

            for (var entityIndex = chunkSize - 1; entityIndex >= 0; --entityIndex) {{

                {getComponents}
                iForEach.Update({insertParams});
            }}
        }}
    }}
}}
";
            builder.AppendLine(methodTemplate);
        }

        // Methods with default T
        for (var index = 0; index <= amount; index++)
        {
            var index1 = index;
            var generics = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                generics.Append($"T{localIndex},");
            generics.Length--;

            var getArrays = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getArrays.AppendLine($"var t{localIndex}Array = chunk.GetArray<T{localIndex}>();");

            var getFirstElement = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getFirstElement.AppendLine($"ref var t{localIndex}FirstElement = ref ArrayExtensions.DangerousGetReference(t{localIndex}Array);");

            var getComponents = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getComponents.AppendLine($"ref var t{localIndex}Component = ref Unsafe.Add(ref t{localIndex}FirstElement, entityIndex);");

            var insertParams = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                insertParams.Append($"ref t{localIndex}Component,");
            insertParams.Length--;

            var methodTemplate = $@"
public partial class World{{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HPQuery<T,{generics}>(in QueryDescription description) where T : struct, IForEach<{generics}>{{
        
        var t = new T();

        var query = Query(in description);
        foreach (ref var chunk in query.GetChunkIterator()) {{ 

            var chunkSize = chunk.Size;
            {getArrays}
            
            {getFirstElement}

            for (var entityIndex = chunkSize - 1; entityIndex >= 0; --entityIndex) {{

                {getComponents}
                t.Update({insertParams});
            }}
        }}
    }}
}}
";
            builder.AppendLine(methodTemplate);
        }
    }

    public static void AppendEntityQueryInterfaceMethods(this StringBuilder builder, int amount)
    {
        for (var index = 0; index <= amount; index++)
        {
            var index1 = index;
            var generics = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                generics.Append($"T{localIndex},");
            generics.Length--;

            var getArrays = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getArrays.AppendLine($"var t{localIndex}Array = chunk.GetArray<T{localIndex}>();");

            var getFirstElement = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getFirstElement.AppendLine($"ref var t{localIndex}FirstElement = ref ArrayExtensions.DangerousGetReference(t{localIndex}Array);");

            var getComponents = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getComponents.AppendLine($"ref var t{localIndex}Component = ref Unsafe.Add(ref t{localIndex}FirstElement, entityIndex);");

            var insertParams = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                insertParams.Append($"ref t{localIndex}Component,");
            insertParams.Length--;

            var methodTemplate = $@"
public partial class World{{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HPEQuery<T,{generics}>(in QueryDescription description, ref T iForEach) where T : struct, IForEachWithEntity<{generics}>{{
        
        var query = Query(in description);
        foreach (ref var chunk in query.GetChunkIterator()) {{ 

            var chunkSize = chunk.Size;
            {getArrays}

            ref var entityFirstElement = ref ArrayExtensions.DangerousGetReference(chunk.Entities);
            {getFirstElement}

            for (var entityIndex = chunkSize - 1; entityIndex >= 0; --entityIndex) {{

                ref readonly var entity = ref Unsafe.Add(ref entityFirstElement, entityIndex);
                {getComponents}
                iForEach.Update(in entity, {insertParams});
            }}
        }}
    }}
}}
";
            builder.AppendLine(methodTemplate);
        }

        // Methods with default T
        for (var index = 0; index <= amount; index++)
        {
            var index1 = index;
            var generics = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                generics.Append($"T{localIndex},");
            generics.Length--;

            var getArrays = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getArrays.AppendLine($"var t{localIndex}Array = chunk.GetArray<T{localIndex}>();");

            var getFirstElement = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getFirstElement.AppendLine($"ref var t{localIndex}FirstElement = ref ArrayExtensions.DangerousGetReference(t{localIndex}Array);");

            var getComponents = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                getComponents.AppendLine($"ref var t{localIndex}Component = ref Unsafe.Add(ref t{localIndex}FirstElement, entityIndex);");

            var insertParams = new StringBuilder();
            for (var localIndex = 0; localIndex <= index1; localIndex++)
                insertParams.Append($"ref t{localIndex}Component,");
            insertParams.Length--;

            var methodTemplate = $@"
public partial class World{{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HPEQuery<T,{generics}>(in QueryDescription description) where T : struct, IForEachWithEntity<{generics}>{{
        
        var t = new T();

        var query = Query(in description);
        foreach (ref var chunk in query.GetChunkIterator()) {{ 

            var chunkSize = chunk.Size;
            {getArrays}

            ref var entityFirstElement = ref ArrayExtensions.DangerousGetReference(chunk.Entities);
            {getFirstElement}

            for (var entityIndex = chunkSize - 1; entityIndex >= 0; --entityIndex) {{

                ref readonly var entity = ref Unsafe.Add(ref entityFirstElement, entityIndex);
                {getComponents}
                t.Update(in entity, {insertParams});
            }}
        }}
    }}
}}
";
            builder.AppendLine(methodTemplate);
        }
    }
}