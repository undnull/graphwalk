# Зависимости для построения графа
import networkx as nx
import matplotlib.pyplot as plt

graph_edges = [
    (0, 1), (0, 2), (0, 3), (0, 5), (0, 7), (1, 3), (1, 6), (1, 7),
    (1, 8), (1, 9), (3, 4), (3, 7), (3, 10), (6, 8), (6, 9), (8, 9)
]

# Вывод графа
g = nx.Graph()
g.add_edges_from(graph_edges)
g_pos = nx.spring_layout(g, seed=0)
nx.draw_networkx(g, pos=g_pos)
plt.show()

# nx.draw_networkx_edges(g, )

node_calc = 0
for item in graph_edges:
    while node_calc < max(item[0] + 1, item[1] + 1):
        node_calc += 1

num_mark = [0] * node_calc
parents = [None] * node_calc
l_params = [None] * node_calc
forward_edges = []
backward_edges = []
articulation_points = []
first_out = None
start = 0  # Выбираем корневую вершину


###################################################
#  Проверка на наличие в списке обратных связей   #
###################################################
def is_in_backward_edges(_edge):
    global backward_edges
    for b_edge in backward_edges:
        if _edge == b_edge or (_edge[1] == b_edge[0] and _edge[0] == b_edge[1]):
            return True
    return False


###################################################
#            Поиск в глубину рекурсией            #
###################################################
def depth_first_search(_node, _parent=None, _counter=0):
    global num_mark, parents, forward_edges
    global backward_edges, graph_edges, first_out

    if num_mark[_node] != 0:
        if not is_in_backward_edges((_node, _parent)):
            backward_edges.append((_node, _parent))  # Добавляем в список обратных граней
        return _counter

    _counter += 1
    num_mark[_node] = _counter
    parents[_node] = _parent
    if _parent is not None:
        forward_edges.append((_parent, _node))  # Добавляем в список обычных граней
    for (edge_parent, edge_node) in graph_edges:
        if edge_parent == _node and edge_node != parents[_node]:
            _counter = depth_first_search(edge_node, _node, _counter)
        elif edge_node == _node and edge_parent != parents[_node]:
            _counter = depth_first_search(edge_parent, _node, _counter)

    if first_out is None:
        first_out = _counter  # Сохраняем первую вершину, на которой отработал DFS
    return _counter


###################################################
# Возвращает список родителей для вершины #
###################################################
def find_parents(_node):
    _parents = []
    parent_iterator = _node
    while not parents[parent_iterator] is None:
        _parents.append(parents[parent_iterator])
        parent_iterator = parents[parent_iterator]
    return _parents


###################################################
# Возвращает список детей (в глубину) для вершины #
###################################################
def find_children(_node, split_by=1):
    global forward_edges
    _children = []
    for _forward_edge in forward_edges:
        if _forward_edge[0] == _node:
            if split_by == 0:
                _children.append(_forward_edge[1])
                _children.extend(find_children(_forward_edge[1], 0 if split_by - 1 < 0 else split_by - 1))
            else:
                tmp = []
                tmp.extend(find_children(_forward_edge[1], 0 if split_by - 1 < 0 else split_by - 1))
                tmp.append(_forward_edge[1])
                _children.append(tmp)
    return _children


###################################################
#     L параметры                                 #
#     Функция находит L параметр для вершины      #
###################################################
def calc_l_param(_node):
    global l_params, parents, num_mark
    if l_params[_node] is None:
        r_sum = [num_mark[_node]]

        _children = find_children(_node, 0)
        for child in _children:
            r_sum.append(calc_l_param(child))

        _parent = parents[_node]
        while _parent is not None:
            for _edge in backward_edges:
                if (_edge[0] == _parent and _edge[1] == _node) or \
                        (_edge[1] == _parent and _edge[0] == _node):
                    r_sum.append(num_mark[_parent])
            _parent = parents[_parent]

        l_params[_node] = min(r_sum)
    return l_params[_node]


#######################################
# Точка сочленения                    #
# Вершины находящиеся в поддереве     #
# от точки сочленения не должны быть  #
# связаны обратными связями с её      #
# родителями                          #
#######################################
def articulation_point(_node, std_out=False):
    global backward_edges, parents

    _parents = find_parents(_node)
    _children = find_children(_node)

    if std_out:
        print('for', _node, 'parents =', _parents, 'children =', _children)

    # Проверяем, чтобы у вершины были дети
    if len(_children) == 0:
        return False
    checked = [True] * len(_children)
    for idx, child_node in enumerate(_children):  # Рассматриваем отдельные поддеревья
        for sub_child_node in child_node:
            for parent_node in _parents:
                for (backward_node1, backward_node2) in backward_edges:
                    if (sub_child_node == backward_node1 and parent_node == backward_node2) or \
                            (parent_node == backward_node1 and sub_child_node == backward_node2):
                        checked[idx] = False  # Если хотя бы один из элементов имеет связь с одним из родителей,
                        # то помечаем, что это не точка сочленения для текущего поддерева
                        break
                if not checked[idx]:
                    break
            if not checked[idx]:
                break
    for check in checked:
        if check:
            return True
    return False


depth_first_search(start)

###################################################
#           Вывод полученной информации           #
###################################################
print('Ребра графа =', graph_edges)
print('Их кол-во =', len(graph_edges))
print()
print('num_mark =', num_mark)
print('parents =', parents)
print()
print('Прямые ребра =', forward_edges, 'их кол-во =', len(forward_edges))
print('Обратные ребра =', backward_edges, 'их кол-во =', len(backward_edges))
print()
print('Вершина, на которой первый раз закончилась работа DFS алгоритма =', first_out)
print()
for i in range(node_calc):
    print("L[" + str(i) + "] =", calc_l_param(i))
print()

for p_idx, p in enumerate(parents):
    if p is not None:
        if p not in articulation_points and l_params[p_idx] >= num_mark[p]:
            if p == start and parents.count(p) <= 1:
                continue
            articulation_points.append(p)
articulation_points.sort()
print('Точки сочленения =', articulation_points)
