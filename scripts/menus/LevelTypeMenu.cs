using Godot;

public class LevelTypeMenu : MenuTemplates
{
    public override void _Ready()
    {   
        base._Ready();
        VBoxContainer easy = (VBoxContainer)FindNode("Easy");
        TextureButton firstbutton = (TextureButton)easy.FindNode("Easy");
        firstbutton.GrabFocus();
    }

}
