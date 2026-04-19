//2452769 ÐÒ¿Éº¯ ¼Æ¿Æ

bool ccu_isUpper(char c)
{
	return c >= 'A' && c <= 'Z';
}

bool ccu_isLower(char c)
{
	return c >= 'a' && c <= 'z';
}

bool ccu_isDigit(char c)
{
	return c >= '0' && c <= '9';
}

bool ccu_isAlpha(char c)
{
	return ccu_isUpper(c) || ccu_isLower(c);
}


char ccu_toUpper(char c)
{
	if (ccu_isLower(c))
		return c - ('a' - 'A');
	else
		return c;
}

char ccu_toLower(char c)
{
	if (ccu_isUpper(c))
		return c + ('a' - 'A');
	else
		return c;
}