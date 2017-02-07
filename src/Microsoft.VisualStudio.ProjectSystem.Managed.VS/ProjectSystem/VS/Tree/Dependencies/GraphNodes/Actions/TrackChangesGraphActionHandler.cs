﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    [Export(typeof(IDependenciesGraphActionHandler))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class TrackChangesGraphActionHandler : GraphActionHandlerBase
    {
        public const int Order = 130;

        [ImportingConstructor]
        public TrackChangesGraphActionHandler(IDependenciesGraphBuilder builder,
                                              IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
            : base(builder, aggregateSnapshotProvider)
        {
        }

        public override bool CanHandleChanges()
        {
            return true;
        }

        public override bool HandleChanges(IGraphContext graphContext, SnapshotChangedEventArgs changes)
        {
            var snapshot = changes.Snapshot;
            if (snapshot == null)
            {
                return false;
            }

            foreach (var inputGraphNode in graphContext.InputNodes.ToList())
            {
                var existingDependency = inputGraphNode.GetValue<IDependency>(
                                            DependenciesGraphSchema.DependencyProperty);
                if (existingDependency == null)
                {
                    continue;
                }

                var projectPath = inputGraphNode.Id.GetValue(CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                var viewProvider = ViewProviders.FirstOrDefault(x => x.Value.SupportsDependency(existingDependency));
                if (viewProvider == null)
                {
                    continue;
                }

                if (!viewProvider.Value.ShouldTrackChanges(projectPath, snapshot.ProjectPath, existingDependency))
                {
                    continue;
                }

                var updatedDependency = GetDependency(projectPath, existingDependency.Id);
                if (updatedDependency == null)
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    viewProvider.Value.TrackChanges(
                        graphContext, projectPath, existingDependency, updatedDependency, inputGraphNode);

                    scope.Complete();
                }
            }

            return false;
        }
    }
}
