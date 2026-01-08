import bpy
import json
import os
def get_voxel_data(obj):
    voxels = {} 
    mesh = obj.to_mesh()
    matrix = obj.matrix_world
    for poly in mesh.polygons:
        world_pos = matrix @ poly.center
        vx = int(round(world_pos.x))
        vy = int(round(world_pos.z)) 
        vz = int(round(world_pos.y))
        key = (vx, vy, vz)
        voxels[key] = 21 

    obj.to_mesh_clear()
    return [{"x": k[0], "y": k[1], "z": k[2], "m": v} for k, v in voxels.items()]
class KogamaExportSettings(bpy.types.PropertyGroup):
    save_path: bpy.props.StringProperty(
        name="Output Path",
        description="Where to save json",
        default="//anim_data.json",
        subtype='FILE_PATH'
    )
    frame_dur: bpy.props.FloatProperty(
        name="Duration (s)",
        description="Time per frame",
        default=0.1,
        min=0.01
    )
class OPS_OT_ExportKogamaAnim(bpy.types.Operator):
    bl_idname = "ops.export_kogama_anim"
    bl_label = "Export Animation"
    bl_description = "Exports current object animation to JSON"

    def execute(self, context):
        scene = context.scene
        props = scene.kogama_props
        obj = context.active_object
        
        if not obj:
            self.report({'ERROR'}, "no active object selected bro")
            return {'CANCELLED'}
        filepath = bpy.path.abspath(props.save_path)
        
        anim_frames = []
        start = scene.frame_start
        end = scene.frame_end
        
        print(f"starting export from {start} to {end}")
        
        for f in range(start, end + 1):
            scene.frame_set(f)
            
            cubes = get_voxel_data(obj)
            
            frame_data = {
                "id": f,
                "dur": props.frame_dur,
                "cubes": cubes
            }
            anim_frames.append(frame_data)
        try:
            with open(filepath, 'w') as outfile:
                json.dump(anim_frames, outfile)
            self.report({'INFO'}, f"saved to {filepath}")
        except Exception as e:
            self.report({'ERROR'}, str(e))
            return {'CANCELLED'}
            
        return {'FINISHED'}
class VIEW3D_PT_KogamaPanel(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Kogama Tools'
    bl_label = 'Stop Motion Export'

    def draw(self, context):
        layout = self.layout
        props = context.scene.kogama_props
        
        col = layout.column()
        col.prop(props, "save_path")
        col.prop(props, "frame_dur")
        
        layout.separator()
        layout.operator("ops.export_kogama_anim", icon='EXPORT')
classes = (
    KogamaExportSettings,
    OPS_OT_ExportKogamaAnim,
    VIEW3D_PT_KogamaPanel
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.kogama_props = bpy.props.PointerProperty(type=KogamaExportSettings)

def unregister():
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.kogama_props

if __name__ == "__main__":
    register()
