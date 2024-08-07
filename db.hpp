// This file is part of ArtadoBot Crawler.
// 
// ArtadoBot Crawler is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option) any
// later version.
// 
// ArtadoBot Crawler is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more
// details.
// 
// You should have received a copy of the GNU General Public License along with
// ArtadoBot Crawler. If not, see <https://www.gnu.org/licenses/>.
#pragma once
#include <sql.h>

#define SQL_FAILED(rc) (((rc) & (~1)) != 0)

SQLRETURN sql_initialize();
void sql_cleanup();