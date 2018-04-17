package com.jetbrains.rider.plugins.nuke

import com.intellij.execution.DefaultExecutionTarget
import com.intellij.execution.ExecutionManager
import com.intellij.execution.RunManager
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.executors.DefaultRunExecutor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.io.FileUtil
import com.jetbrains.rider.model.rdNukeModel
import com.jetbrains.rider.model.runnableProjectsModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.project.DotNetProjectConfiguration
import com.jetbrains.rider.run.configurations.project.DotNetProjectConfigurationType
import com.jetbrains.rider.util.idea.LifetimedProjectComponent

class RunConfigurationManager(project: Project, runManager: RunManager) : LifetimedProjectComponent(project) {
    init {
        project.solution.rdNukeModel.build.advise(componentLifetime) { it ->
            var configuration = runManager.findConfigurationByName(it.target)
            if (configuration == null) {
                val configurationType = runManager.configurationFactories.single { it -> it is DotNetProjectConfigurationType }
                val configurationFactory = configurationType.configurationFactories.first()
                configuration = runManager.createRunConfiguration(it.target, configurationFactory)
                configuration.isTemporary = true

                val buildProjectFile = FileUtil.toSystemIndependentName(it.projectFile)
                var dotnetProject = project.solution.runnableProjectsModel.projects.valueOrNull!!
                        .single { it -> it.projectFilePath == buildProjectFile }

                val dotnetConfiguration = configuration.configuration as DotNetProjectConfiguration
                dotnetConfiguration.parameters.projectFilePath = dotnetProject.projectFilePath
                dotnetConfiguration.parameters.projectKind = dotnetProject.kind
                dotnetConfiguration.parameters.programParameters = it.target
                if (it.skipDependencies)
                    dotnetConfiguration.parameters.programParameters += " -skip"

                configuration!!.checkSettings()

                runManager.addConfiguration(configuration)
            }

            runManager.selectedConfiguration = configuration
            val executionManager = ExecutionManager.getInstance(project)
            val executor = if (it.debugMode) DefaultDebugExecutor.getDebugExecutorInstance() else DefaultRunExecutor.getRunExecutorInstance()
            executionManager.restartRunProfile(
                    project,
                    executor,
                    DefaultExecutionTarget.INSTANCE,
                    configuration,
                    null)
        }
    }
}