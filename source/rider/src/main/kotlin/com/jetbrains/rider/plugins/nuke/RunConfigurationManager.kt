package com.jetbrains.rider.plugins.nuke

import com.intellij.execution.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.executors.DefaultRunExecutor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.io.FileUtil
import com.jetbrains.rider.model.BuildInvocation
import com.jetbrains.rider.model.rdNukeModel
import com.jetbrains.rider.model.runnableProjectsModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.project.DotNetProjectConfiguration
import com.jetbrains.rider.run.configurations.project.DotNetProjectConfigurationType
import com.jetbrains.rider.util.idea.LifetimedProjectComponent

class RunConfigurationManager(project: Project, private val runManager: RunManager) : LifetimedProjectComponent(project) {
    init {
        project.solution.rdNukeModel.build.advise(componentLifetime) { buildInvocation ->
            val configuration = runManager.findConfigurationByName(getConfigurationName(buildInvocation))
                    ?: createConfigurationAndSettings(buildInvocation)
            runManager.selectedConfiguration = configuration
            ExecutionManager.getInstance(project)
                    .restartRunProfile(
                            project,
                            getExecutor(buildInvocation),
                            DefaultExecutionTarget.INSTANCE,
                            configuration,
                            null)
        }
    }

    private fun getConfigurationName(buildInvocation: BuildInvocation): String {
        return if (buildInvocation.skipDependencies) {
            "${buildInvocation.target} (without dependencies)"
        } else {
            buildInvocation.target
        }
    }

    private fun getExecutor(buildInvocation: BuildInvocation): Executor {
        return if (buildInvocation.debugMode)
            DefaultDebugExecutor.getDebugExecutorInstance() else DefaultRunExecutor.getRunExecutorInstance()
    }

    private fun createConfigurationAndSettings(buildInvocation: BuildInvocation): RunnerAndConfigurationSettings {
        val configurationType = runManager.configurationFactories.single { it -> it is DotNetProjectConfigurationType }
        val configurationFactory = configurationType.configurationFactories.first()
        val configuration = runManager
                .createRunConfiguration(getConfigurationName(buildInvocation), configurationFactory)

        configuration.isTemporary = true

        val buildProjectFile = FileUtil.toSystemIndependentName(buildInvocation.projectFile)
        val dotNetProject = project.solution.runnableProjectsModel.projects.valueOrNull!!
                .single { it -> it.projectFilePath == buildProjectFile }

        val dotNetConfiguration = configuration.configuration as DotNetProjectConfiguration
        dotNetConfiguration.parameters.projectFilePath = dotNetProject.projectFilePath
        dotNetConfiguration.parameters.projectKind = dotNetProject.kind
        dotNetConfiguration.parameters.programParameters = buildInvocation.target
        if (buildInvocation.skipDependencies)
            dotNetConfiguration.parameters.programParameters += " -skip"

        configuration.checkSettings()

        runManager.addConfiguration(configuration)
        return configuration
    }
}