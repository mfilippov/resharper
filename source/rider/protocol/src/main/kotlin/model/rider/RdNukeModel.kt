package model.rider

import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.generator.nova.PredefinedType.bool
import com.jetbrains.rider.generator.nova.PredefinedType.string
import com.jetbrains.rider.model.nova.ide.SolutionModel
import org.jetbrains.kotlin.ir.expressions.IrConstKind

@Suppress("unused")
object RdNukeModel : Ext(SolutionModel.Solution) {

    val BuildInvocation = structdef {
        field("projectFile", string)
        field("target", string)
        field("debugMode", bool)
        field("skipDependencies", bool)
    }

    init {
        signal("build", BuildInvocation)
    }

}