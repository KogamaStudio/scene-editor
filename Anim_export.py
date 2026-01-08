import bpy
import json
import mathutils

def get_transform_data(obj):
    matrix = obj.matrix_world
    loc, rot, scale = matrix.decompose()
    out_pos = {
        "x": round(loc.x, 4),
        "y": round(loc.z, 4),
        "z": round(loc.y, 4)
    }
    out_rot = {
        "x": round(rot.x, 4),
        "y": round(rot.z, 4),
        "z": round(rot.y, 4),
        "w": round(rot.w * -1, 4) 
    }
    
    return {"pos": out_pos, "rot": out_rot}

class KogamaAnimSettings(bpy.types.PropertyGroup):
    save_path: bpy.props.StringProperty(
        name="Output Path",
        default="//anim_transform.json",
        subtype='FILE_PATH'
    )
    frame_dur: bpy.props.FloatProperty(
        name="Duration (s)",
        default=0.1,
        min=0.01
    )

class OPS_OT_ExportKogamaTransform(bpy.types.Operator):
    bl_idname = "ops.export_kogama_transform"
    bl_label = "Export Transforms"
    
    def execute(self, context):
        scene = context.scene
        props = scene.kogama_anim_props
        obj = context.active_object
        
        if not obj:
            self.report({'ERROR'}, "select an object")
            return {'CANCELLED'}
            
        filepath = bpy.path.abspath(props.save_path)
        frames_data = []
        
        start = scene.frame_start
        end = scene.frame_end
        
        print(f"exporting {obj.name} from {start} to {end}")
        
        for f in range(start, end + 1):
            scene.frame_set(f)
            t_data = get_transform_data(obj)
            
            frame = {
                "id": f,
                "dur": props.frame_dur,
                "pos": t_data["pos"],
                "rot": t_data["rot"]
            }
            frames_data.append(frame)
            
        try:
            with open(filepath, 'w') as outfile:
                json.dump(frames_data, outfile)
            self.report({'INFO'}, f"saved to {filepath}")
        except Exception as e:
            self.report({'ERROR'}, str(e))
            return {'CANCELLED'}
            
        return {'FINISHED'}

class VIEW3D_PT_KogamaTransformPanel(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Kogama Tools'
    bl_label = 'Transform Export'
    
    def draw(self, context):
        layout = self.layout
        props = context.scene.kogama_anim_props
        
        col = layout.column(align=True)
        col.prop(props, "save_path")
        col.prop(props, "frame_dur")
        
        layout.separator()
        row = layout.row()
        row.scale_y = 1.5
        row.operator("ops.export_kogama_transform", text="Export Animation", icon='EXPORT')

classes = (
    KogamaAnimSettings,
    OPS_OT_ExportKogamaTransform,
    VIEW3D_PT_KogamaTransformPanel
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.kogama_anim_props = bpy.props.PointerProperty(type=KogamaAnimSettings)

def unregister():
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.kogama_anim_props

if __name__ == "__main__":
    register()
