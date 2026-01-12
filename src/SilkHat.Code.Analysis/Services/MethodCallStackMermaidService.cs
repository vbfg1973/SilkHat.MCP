using SilkHat.Code.Analysis.Abstractions;
using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Services
{
    public sealed class MethodCallStackMermaidService : IMethodCallStackMermaidService
    {
        public string BuildDiagram(IReadOnlyList<MethodCallStackNode> nodes)
        {
            var lines = new List<string> { "sequenceDiagram" };
            var participants = new Dictionary<string, string>(StringComparer.Ordinal);
            var resolvedInterfaceLabels = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var node in nodes)
            {
                if (!node.IsInterfaceTarget
                    || string.IsNullOrWhiteSpace(node.InterfaceTypeName)
                    || string.IsNullOrWhiteSpace(node.ResolvedTypeName))
                    continue;

                resolvedInterfaceLabels[node.ResolvedTypeName] = BuildInterfaceLabel(
                    node.InterfaceTypeName,
                    node.ResolvedTypeName);
            }

            var nodeLookup = nodes.GroupBy(node => node.ParentNodeId ?? string.Empty)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

            void RenderNode(MethodCallStackNode node)
            {
                var callerLabel = GetCallerLabel(node, resolvedInterfaceLabels);
                var calleeLabel = GetCalleeLabel(node, resolvedInterfaceLabels);

                var callerAlias = GetOrAddParticipant(participants, callerLabel, lines);
                var calleeAlias = GetOrAddParticipant(participants, calleeLabel, lines);

                lines.Add($"{callerAlias}->>{calleeAlias}: {node.MethodName}");

                if (nodeLookup.TryGetValue(node.NodeId, out var children))
                    foreach (var child in children)
                        RenderNode(child);

                lines.Add($"{calleeAlias}-->>{callerAlias}: return");
            }

            if (nodeLookup.TryGetValue(string.Empty, out var roots))
                foreach (var node in roots)
                    RenderNode(node);

            return string.Join('\n', lines);
        }

        private static string GetCallerLabel(
            MethodCallStackNode node,
            IReadOnlyDictionary<string, string> resolvedInterfaceLabels)
        {
            var callerFullName = GetFullTypeName(node.CallerNamespace, node.CallerTypeName);
            if (!string.IsNullOrWhiteSpace(callerFullName)
                && resolvedInterfaceLabels.TryGetValue(callerFullName, out var mappedLabel))
                return mappedLabel;

            return ShortTypeName(callerFullName);
        }

        private static string GetCalleeLabel(
            MethodCallStackNode node,
            IReadOnlyDictionary<string, string> resolvedInterfaceLabels)
        {
            if (node.IsInterfaceTarget && !string.IsNullOrWhiteSpace(node.InterfaceTypeName))
            {
                if (!string.IsNullOrWhiteSpace(node.ResolvedTypeName))
                    return BuildInterfaceLabel(node.InterfaceTypeName, node.ResolvedTypeName);

                return ShortTypeName(node.InterfaceTypeName);
            }

            var calleeFullName = GetFullTypeName(node.Namespace, node.TypeName);
            if (!string.IsNullOrWhiteSpace(calleeFullName)
                && resolvedInterfaceLabels.TryGetValue(calleeFullName, out var mappedLabel))
                return mappedLabel;

            return ShortTypeName(calleeFullName);
        }

        private static string BuildInterfaceLabel(string interfaceTypeName, string resolvedTypeName)
        {
            return $"{ShortTypeName(interfaceTypeName)}\\n*({ShortTypeName(resolvedTypeName)})*";
        }

        private static string GetFullTypeName(string? namespaceName, string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return string.Empty;

            if (string.IsNullOrWhiteSpace(namespaceName)) return typeName;

            return $"{namespaceName}.{typeName}";
        }

        private static string ShortTypeName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;

            var lastDot = fullName.LastIndexOf('.');
            return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
        }

        private static string GetOrAddParticipant(
            IDictionary<string, string> participants,
            string label,
            ICollection<string> lines)
        {
            if (participants.TryGetValue(label, out var existing)) return existing;

            var aliasBase = new string(label.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(aliasBase)) aliasBase = "Participant";

            var alias = aliasBase;
            var index = 1;
            while (participants.Values.Contains(alias, StringComparer.Ordinal))
            {
                alias = $"{aliasBase}{index}";
                index++;
            }

            participants[label] = alias;
            lines.Add($"participant {alias} as \"{label}\"");
            return alias;
        }
    }
}