//2452769 幸可函 计科
#pragma once
/**
 * @brief  判断是否为大写字母
 * @param  待判断的字符
 * @return 是大写字母返回 true，否则返回 false
 */
bool ccu_isUpper(char c);
/**
 * @brief  判断是否为小写字母
 * @param  待判断的字符
 * @return 是小写字母返回 true，否则返回 false
 */
bool ccu_isLower(char c);
/**
 * @brief  判断是否为字母
 * @param  待判断的字符
 * @return 是字母返回 true，否则返回 false
 */
bool ccu_isAlpha(char c);
/**
 * @brief  将小写字母转换为大写字母
 * @param  待转换的字符
 * @return 转换后的字符（如果不是小写字母则不变）
 */
char ccu_toUpper(char c);
/**
 * @brief  将大写字母转换为小写字母
 * @param  待转换的字符
 * @return 转换后的字符（如果不是大写字母则不变）
 */
char ccu_toLower(char c);
/**
 * @brief  判断是否为数字
 * @param  待判断的字符
 * @return 是数字返回 true，否则返回 false
 */
bool ccu_isDigit(char c);