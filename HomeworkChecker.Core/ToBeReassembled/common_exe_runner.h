/* 2452769 幸可函 计科 */
#pragma once

#define _CRT_SECURE_NO_WARNINGS
#include <iostream>
#include <sstream>
#include <string>
#include <Windows.h>
using namespace std;
#define TIMER_NEW_VERSION 1

#if TIMER_NEW_VERSION
// 新版本定时器（CreateTimerQueueTimer函数及相关配套函数，适用于多线程）
// 仅<Windows.h>即可，不需要其它头文件
#else
// 旧版本定时器（timeSetEvent函数及相关配套函数，不适用多线程）
#include <mmsystem.h>
#pragma comment(lib,"Winmm.lib")
#endif

/* 定义各种返回类型 */
enum class CheckExec_Errno {
	ok = 0,
	create_timer_id_failed,		//创建定时器ID失败
	popen_faliled,				//管道方式打开可执行文件失败
	start_timer_failed,			//启动定时器失败
	timeout,					//超时
	max_output,					//达到设定的输出上限（死循环输出，反正肯定不对了）
	killed_by_callback,			//死循环（超时且fgetc阻塞）
	max
};

/* CheckExec_Errno 的输出 */
ostream& operator<<(ostream& out, const CheckExec_Errno& eno);

class exe_runner {
protected:
	/* 初始化的4个参数 */
	string full_exec_cmd;	//完整的执行命令（用于_popen）
	string exec_name;		//exe文件名（用于taskkill）
	int    cfg_timeout;		//设置的超时（秒）
	int    max_output_len;	//读取输出的最大长度

	FILE* fp_exe;
	int    time_count;

#if TIMER_NEW_VERSION
	HANDLE timer_id; //定时器ID
#else
	MMRESULT timer_id; //定时器ID
#endif
	LARGE_INTEGER time_tick;
	LARGE_INTEGER begin_time;
	LARGE_INTEGER end_time;

	CheckExec_Errno eno;  //错误号（不能叫errno）

	int  start_timer();
	void stop_timer();
	int  stop(CheckExec_Errno eno);

public:
	ostringstream msg; //存放输出

	int              running();
	double           get_running_time() const;
	string           get_full_cmd_exec() const;
	CheckExec_Errno  get_errno() const;
	int              reset(); //重置，进行下次运行

	exe_runner(const string& full_exec_cmd, const string& exec_name, int max_output_len, int timeout_second);
	~exe_runner();

	/* 回调函数声明为友元 */
#if TIMER_NEW_VERSION
	friend void CALLBACK timeout_process(PVOID lpParameter, BOOLEAN TimerOrWaitFired);
#else
	friend void CALLBACK timeout_process(UINT uTimerID, UINT uMsg, DWORD dwUser, DWORD dw1, DWORD dw2);
#endif
};

#if TIMER_NEW_VERSION
void CALLBACK timeout_process(PVOID ExtParameter, BOOLEAN TimerOrWaitFired);
#else
void CALLBACK timeout_process(UINT uTimerID, UINT uMsg, DWORD ExtParameter, DWORD dw1, DWORD dw2);
#endif