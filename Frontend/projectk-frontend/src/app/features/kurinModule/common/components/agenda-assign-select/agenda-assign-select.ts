import { ChangeDetectionStrategy, Component, computed, inject, input, model, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TreeSelectModule } from 'primeng/treeselect';
import { TreeNode } from 'primeng/api';
import { AgendaService } from '../../services/agenda-service/agenda-service';
import { AgendaAssignTargets, AgendaTargetInput, AgendaTargetType } from '../../models/agenda';

interface TargetNodeData {
  targetType: AgendaTargetType;
  targetKey: string;
}

/**
 * «Призначити для»: дерево Курінь → Гуртки → Мембери з пошуком (`filter`). Бекенд уже віддає лише ті
 * цілі, які поточний користувач має право призначати, тож тут немає власної перевірки прав.
 */
@Component({
  selector: 'app-agenda-assign-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, TreeSelectModule],
  templateUrl: './agenda-assign-select.html',
  styleUrl: './agenda-assign-select.css'
})
export class AgendaAssignSelectComponent implements OnInit {
  private readonly agendaService = inject(AgendaService);

  readonly kurinKey = input.required<string>();
  /** Two-way selection as backend target inputs. */
  readonly targets = model<AgendaTargetInput[]>([]);

  protected readonly nodes = signal<TreeNode<TargetNodeData>[]>([]);
  protected selectedNodes: TreeNode<TargetNodeData>[] = [];

  private readonly nodeByKey = computed(() => {
    const map = new Map<string, TreeNode<TargetNodeData>>();
    const walk = (list: TreeNode<TargetNodeData>[]) => {
      for (const node of list) {
        if (node.key) {
          map.set(node.key, node);
        }
        if (node.children) {
          walk(node.children);
        }
      }
    };
    walk(this.nodes());
    return map;
  });

  ngOnInit(): void {
    this.agendaService.getAssignTargets(this.kurinKey()).subscribe(tree => {
      this.nodes.set(this.buildNodes(tree));
      this.syncSelectionFromTargets();
    });
  }

  onSelectionChange(selected: TreeNode<TargetNodeData>[]): void {
    const targets = (selected ?? [])
      .map(node => node.data)
      .filter((data): data is TargetNodeData => !!data)
      .map(data => ({ targetType: data.targetType, targetKey: data.targetKey }));
    this.targets.set(targets);
  }

  private buildNodes(tree: AgendaAssignTargets): TreeNode<TargetNodeData>[] {
    const roots: TreeNode<TargetNodeData>[] = [];

    if (tree.canTargetKurin) {
      roots.push({
        key: `kurin:${tree.kurinKey}`,
        label: tree.kurinLabel,
        icon: 'pi pi-flag',
        selectable: true,
        data: { targetType: 'Kurin', targetKey: tree.kurinKey }
      });
    }

    for (const group of tree.groups) {
      roots.push({
        key: `group:${group.groupKey}`,
        label: group.name,
        icon: 'pi pi-sitemap',
        selectable: group.canTargetGroup,
        data: { targetType: 'Group', targetKey: group.groupKey },
        children: group.members.map(member => ({
          key: `member:${member.memberKey}`,
          label: member.fullName,
          icon: 'pi pi-user',
          selectable: true,
          data: { targetType: 'Member' as AgendaTargetType, targetKey: member.memberKey }
        }))
      });
    }

    return roots;
  }

  private syncSelectionFromTargets(): void {
    const map = this.nodeByKey();
    const initial = this.targets();
    const selected: TreeNode<TargetNodeData>[] = [];
    for (const target of initial) {
      const key = `${target.targetType.toLowerCase()}:${target.targetKey}`;
      const node = map.get(key);
      if (node) {
        selected.push(node);
      }
    }
    this.selectedNodes = selected;
  }
}
