# Copilot Instructions

## 项目指南
- 用户是计算机专业大学生，项目目标是把课程中的CLI比对流程（运行demo/学生程序并比较输出）做成基于C# WPF的GUI工具。
- 用户偏好学习型讲解，且希望代码尽量都带注释（尤其模板学习阶段）。
- 用户偏好代码修改尽量保持简洁且有组织。
- 用户偏好减少冗余状态，倾向只保留单一主题选择状态而不是并存多个主题状态字段。
- 用户倾向按设置大类拆分 ViewModel（如 Personalization 与 About），避免将所有设置项集中在单个 SettingsViewModel。
- 用户希望暂缓实现主题色更改逻辑，后续通过独立的 ColorPickerDialog 或 ColorPickerControl 实现。