/* 2452769 幸可函 计科 */
#define _CRT_SECURE_NO_WARNINGS
#include <iostream>
#include <iomanip>
#include "../include/class_aat.h"
#include "../include/class_cft.h"
#include "../include/class_txt_compare.h"
#include "hw_check_exe.h"
using namespace std;

enum HW_CHECK_ARGS {
	HW_CHECK_HELP = 0,
	HW_CHECK_DEBUG,
	HW_CHECK_CHECKNAME,
	HW_CHECK_CHECKCFG_ONLY,
	HW_CHECK_CFGFILE,
};

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
static void usage(const char* const full_procname)
{
	const int offset_len = 7;
	const char* procname = strrchr(full_procname, '\\');
	if (procname == NULL)
		procname = full_procname;

	/* 本程序的特殊示例 */
	ostringstream msg;
	msg << "e.g.  :" << endl;
	msg << setw(offset_len) << ' ' << procname << " --checkname 3-b3                 : 按配置文件[3-b3]组的设定检查exe的运行结果" << endl;
	msg << setw(offset_len) << ' ' << procname << " --checkname 3-b3 --checkcfg_only : 检查配置文件[3-b3]组的设定是否正确" << endl;
	msg << setw(offset_len) << ' ' << procname << " --checkname 3-b3 --debug trace   : 按配置文件[3-b3]组的设定检查exe的运行结果，打印所有调试信息" << endl;
	msg << endl;

	cout << msg.str() << endl;
}


/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
int main(int argc, char** argv)
{
	const string loglevel_define[] = { "warn", "info", "debug", "trace", "" };

	args_analyse_tools args[] = {
		args_analyse_tools("--help",		   ST_EXTARGS_TYPE::boolean, 0, false),
		args_analyse_tools("--debug",		   ST_EXTARGS_TYPE::str_with_set_default, 1, 0, loglevel_define),
		args_analyse_tools("--checkname",	   ST_EXTARGS_TYPE::str, 1, string("")),
		args_analyse_tools("--checkcfg_only", ST_EXTARGS_TYPE::boolean, 0, false),
		args_analyse_tools("--cfgfile",	   ST_EXTARGS_TYPE::str, 1, string("hw_check_exe.cfg")),
		args_analyse_tools()  //最后一个，用于结束
	};

	int cur_argc;

	//最后一个参数1，表示除选项参数外，还有其它参数
	if ((cur_argc = args_analyse_process(argc, argv, args, 0)) < 0) {
		cout << "\n" << argv[0] << " Version : V2025.12.23" << endl;
		args_analyse_print(args);
		usage(argv[0]);
		return -1;
	}

	/* 对help做特殊处理 */
	if (args[HW_CHECK_HELP].existed()) {
		//只要有 --help，其它参数都忽略，显示帮助即可
		cout << "\n\n" << argv[0] << " Version : V2025.12.23\n" << endl;
		args_analyse_print(args);
		usage(argv[0]);
		return -1; //执行完成直接退出
	}

	/* 必选参数检查：必须指定 --checkname */
	if (!args[HW_CHECK_CHECKNAME].existed()) {
		cout << "\n\n" << argv[0] << " Version : V2025.12.23\n" << endl;
		args_analyse_print(args);
		usage(argv[0]);
		cout << "必须指定参数[" << args[HW_CHECK_CHECKNAME].get_name() << "]" << endl;
		return -1;
	}

	if (args[HW_CHECK_DEBUG].existed())
		args_analyse_print(args);

	/* 进入实际的功能调用，完成相应的功能 */
	string checkname = args[HW_CHECK_CHECKNAME].get_string();
	string debug_type = args[HW_CHECK_DEBUG].get_string();
	bool checkcfg_only = args[HW_CHECK_CHECKCFG_ONLY].existed();
	string cfgfile = args[HW_CHECK_CFGFILE].get_string();

	/* 要求建一个 hw_checker 类，构造函数的参数按下面的顺序排列 */
	hw_checker hc(cfgfile, checkname, debug_type);

	hc.load_config(); //加载配置文件

	if (checkcfg_only) {
		hc.print_cfg_info(); //打印配置文件摘要信息
	}
	//读取.cfg失败则直接退出（错误信息已打印）
	if (hc.get_checkcfg_ret() == -2) {
		return -2;
	}
	//读取成功但配置有误
	if (hc.get_checkcfg_ret() == -1) {
		cout << "\n[--严重错误--] 配置文件存在下列的错误：" << endl;
		hc.print_errors(); //打印错误信息
		return -1;
	}

	if(checkcfg_only) {
		return 0; //仅检查配置文件正确性，完成后直接退出
	}
	//-----------主流程-----------
	if (hc.load_student_list() != 0) { //加载学生名单，失败则直接退出
		hc.print_errors();
		return -1;
	}

	if (hc.get_demo_outputs() != 0) { //缓存demo输出，失败则直接退出
		hc.print_errors();
		return -1;
	}

	//如果学生名单为空，则直接退出
	if (hc.is_student_list_empty()) {
		return -1;
	}

	hc.check_all_students(); //检查所有学生
	hc.save_result_xls(); //保存结果到 xls 文件

	return 0;
}

